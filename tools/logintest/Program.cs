using System.Buffers.Binary;
using System.Net;
using Google.Protobuf;
using Pavane.Common;
using Takasho.Schema.Score.PlayerApi;

// speaks Pavane's Packer-framed gRPC directly to the GameServer on :5001 to verify LoginV1/ShutdownV1 end-to-end (routing + handler + Packer encoding) without the on-device anti-cheat getting in the way.

var handler = new SocketsHttpHandler();
using var http = new HttpClient(handler);

static byte[] Frame(byte[] message)
{
    var packed = Packer.Pack(message);
    var frame = new byte[5 + packed.Length];
    BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1), packed.Length);
    packed.CopyTo(frame, 5);
    return frame;
}

static byte[] Unframe(byte[] response)
{
    var len = BinaryPrimitives.ReadInt32BigEndian(response.AsSpan(1));
    return Packer.Unpack(response.AsSpan(5, len).ToArray());
}

async Task<byte[]> Call(string path, byte[] requestMessage)
{
    var req = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:5001" + path)
    {
        Version = HttpVersion.Version20,
        VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        Content = new ByteArrayContent(Frame(requestMessage)),
    };
    req.Content.Headers.TryAddWithoutValidation("Content-Type", "application/grpc");
    req.Headers.TryAddWithoutValidation("te", "trailers");
    var resp = await http.SendAsync(req);
    resp.EnsureSuccessStatusCode();
    var body = await resp.Content.ReadAsByteArrayAsync();
    return body.Length == 0 ? [] : Unframe(body);
}

Console.WriteLine("== LoginV1 ==");
var loginBytes = await Call("/takasho.schema.score.player_api.Login/LoginV1", new LoginV1.Types.Request().ToByteArray());
if (loginBytes.Length == 0)
{
    Console.WriteLine("EMPTY response (handler not hit!)");
    return 1;
}
var login = LoginV1.Types.Response.Parser.ParseFrom(loginBytes);
Console.WriteLine($"bytes={loginBytes.Length}");
Console.WriteLine($"isNewPlayer={login.IsNewPlayer}");
Console.WriteLine($"common!=null: {login.Common != null}");
Console.WriteLine($"player.id={login.Common?.Player?.PlayerId}  player.name={login.Common?.Player?.Name}  stamina={login.Common?.Player?.Stamina}");
Console.WriteLine($"wallet!=null: {login.Common?.Wallet != null}  tutorial!=null: {login.Common?.TutorialInfo != null}");
Console.WriteLine($"system.serverTime={login.System?.ServerTime}  maintenance={login.System?.Maintenance}");

Console.WriteLine("\n== ShutdownV1 ==");
var shutBytes = await Call("/takasho.schema.score.player_api.Login/ShutdownV1", new ShutdownV1.Types.Request().ToByteArray());
if (shutBytes.Length == 0) { Console.WriteLine("EMPTY response"); return 1; }
var shut = ShutdownV1.Types.Response.Parser.ParseFrom(shutBytes);
Console.WriteLine($"bytes={shutBytes.Length}  shutdownEnable={shut.ShutdownEnable}  serverShutdownTime={shut.ServerShutdownTime}  system!=null:{shut.System != null}");

Console.WriteLine("\nOK");
return 0;
