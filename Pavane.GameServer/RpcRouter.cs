using System.Buffers.Binary;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Pavane.Common;
using Takasho.Schema.CommonFeatureset.PlayerApi;
using ScoreSystemV1 = Takasho.Schema.Score.ResourceCn.System.V1;

namespace Pavane.GameServer;

// serves the client's gRPC calls by hand: each message is Packer-encrypted, so the standard gRPC server stack, which expects plain protobuf, cannot be used directly
public sealed class RpcRouter(ILogger<RpcRouter> log)
{
    private const int FrameHeaderSize = 5;

    private static readonly Dictionary<string, MethodDescriptor> Methods = LoadMethods();
    private readonly Dictionary<string, Func<byte[], byte[]>> handlers = [];

    public void Map<TRequest, TResponse>(string service, string method, Func<TRequest, TResponse> handler)
        where TRequest : IMessage<TRequest>, new()
        where TResponse : IMessage<TResponse>
    {
        var parser = new MessageParser<TRequest>(() => new TRequest());
        handlers[$"/{service}/{method}"] = body => handler(parser.ParseFrom(body)).ToByteArray();
    }

    public async Task Handle(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        var request = await ReadRequest(ctx);

        byte[] response;
        if (handlers.TryGetValue(path, out var handler))
        {
            log.LogDebug("RPC {Path}", path);
            response = handler(request);
        }
        else if (Methods.TryGetValue(path, out var method))
        {
            // no explicit handler yet: return a well-formed, empty response of the correct type with the server-info envelope populated, instead of raw empty bytes
            // many lobby RPCs carry a `system` (SystemInfo) field the client reads; an all-default reply with a valid server time lets the lobby load sequence keep going
            log.LogWarning("Stubbed RPC {Path}", path);
            response = BuildStub(method);
        }
        else
        {
            log.LogWarning("Unknown RPC {Path}: {Request}", path, Convert.ToHexString(request));
            response = [];
        }

        await WriteResponse(ctx, response);
    }

    private static async Task<byte[]> ReadRequest(HttpContext ctx)
    {
        using var buffer = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(buffer);
        var frame = buffer.ToArray();
        if (frame.Length < FrameHeaderSize)
            return [];

        var length = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(1));
        return Packer.Unpack(frame.AsSpan(FrameHeaderSize, length).ToArray());
    }

    private static async Task WriteResponse(HttpContext ctx, byte[] message)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var packed = Packer.Pack(message);

        var header = new byte[FrameHeaderSize];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(1), packed.Length);

        ctx.Response.ContentType = "application/grpc";
        ctx.Response.Headers["x-takasho-requested-timestamp"] = timestamp;
        await ctx.Response.Body.WriteAsync(header);
        await ctx.Response.Body.WriteAsync(packed);

        ctx.Response.AppendTrailer("grpc-status", "0");
        ctx.Response.AppendTrailer("x-takasho-respond-timestamp", timestamp);
    }

    // builds a default instance of the method's response type, populating a `system` (SystemInfo) field with the current server time when present.
    // `common` is left null on purpose, an empty CommonResponse merged client-side could wipe player state.
    private static byte[] BuildStub(MethodDescriptor method)
    {
        var msg = (IMessage)Activator.CreateInstance(method.OutputType.ClrType)!;
        foreach (var field in method.OutputType.Fields.InFieldNumberOrder())
        {
            if (field.FieldType == FieldType.Message
                && field.Name == "system"
                && field.MessageType.ClrType == typeof(ScoreSystemV1.SystemInfo))
            {
                field.Accessor.SetValue(msg, new ScoreSystemV1.SystemInfo
                {
                    ServerTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                });
            }
        }
        return msg.ToByteArray();
    }

    private static string Describe(string path, byte[] request)
    {
        if (!Methods.TryGetValue(path, out var method))
            return Convert.ToHexString(request);

        try
        {
            return method.InputType.Parser.ParseFrom(request).ToString() ?? "";
        }
        catch (InvalidProtocolBufferException)
        {
            return Convert.ToHexString(request);
        }
    }

    // every generated *Reflection class exposes a FileDescriptor listing that file's services
    private static Dictionary<string, MethodDescriptor> LoadMethods() =>
        typeof(SystemReflection).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Reflection", StringComparison.Ordinal))
            .Select(type => type.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null))
            .OfType<FileDescriptor>()
            .SelectMany(file => file.Services)
            .SelectMany(service => service.Methods)
            .ToDictionary(method => $"/{method.Service.FullName}/{method.Name}");
}
