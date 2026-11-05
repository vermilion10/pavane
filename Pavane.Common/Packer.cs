using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Snappier;

namespace Pavane.Common;

// encodes HTTP responses in the format the client's Takasho Packer reads: `hex(nonce[16] || AES-256-CBC(SnappyFrame(hash[32] || body)))` where hash = HMAC-SHA256(key: aesKey || nonce, data: body)
public static class Packer
{
    private const int NonceSize = 16;
    private const int HashSize = 32;
    private const int MaxChunkSize = 65536;
    private const uint CrcMaskDelta = 0xa282ead8;

    // the Takasho CommonKey, shared by every /public route, gRPC, and the master-data password
    internal static readonly byte[] Key = "9yUwgeXTX@uyBB^j%rRbSVDVUjEnHUjh"u8.ToArray();
    private static readonly byte[] StreamIdentifier = [0xff, 0x06, 0x00, 0x00, .. "sNaPpY"u8];

    public static string PackToHex(string json) => Convert.ToHexStringLower(Pack(Encoding.UTF8.GetBytes(json)));

    public static byte[] Pack(byte[] body)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] hashKey = [.. Key, .. nonce];
        var hash = HMACSHA256.HashData(hashKey, body);
        var frame = SnappyFrame([.. hash, .. body]);

        using var aes = Aes.Create();
        aes.Key = Key;
        var encrypted = aes.EncryptCbc(frame, nonce, PaddingMode.PKCS7);
        return [.. nonce, .. encrypted];
    }

    public static byte[] Unpack(byte[] packed)
    {
        var nonce = packed.AsSpan(0, NonceSize).ToArray();

        using var aes = Aes.Create();
        aes.Key = Key;
        var frame = aes.DecryptCbc(packed.AsSpan(NonceSize), nonce, PaddingMode.PKCS7);

        using var input = new MemoryStream(frame);
        using var snappy = new SnappyStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        snappy.CopyTo(output);
        var content = output.ToArray();

        var hash = content.AsSpan(0, HashSize);
        var body = content[HashSize..];
        byte[] hashKey = [.. Key, .. nonce];
        if (!hash.SequenceEqual(HMACSHA256.HashData(hashKey, body)))
            throw new InvalidDataException("Packed message failed its hash check");
        return body;
    }

    // snappy framing format using uncompressed chunks, which every conforming reader accepts
    private static byte[] SnappyFrame(byte[] data)
    {
        using var output = new MemoryStream();
        output.Write(StreamIdentifier);

        Span<byte> header = stackalloc byte[8];
        for (var offset = 0; offset < data.Length; offset += MaxChunkSize)
        {
            var chunk = data.AsSpan(offset, Math.Min(MaxChunkSize, data.Length - offset));
            var chunkLength = chunk.Length + 4; // the length field includes the checksum

            header[0] = 0x01; // uncompressed data chunk
            header[1] = (byte)chunkLength;
            header[2] = (byte)(chunkLength >> 8);
            header[3] = (byte)(chunkLength >> 16);
            BinaryPrimitives.WriteUInt32LittleEndian(header[4..], MaskedCrc32C(chunk));

            output.Write(header);
            output.Write(chunk);
        }

        return output.ToArray();
    }

    private static uint MaskedCrc32C(ReadOnlySpan<byte> data)
    {
        var crc = uint.MaxValue;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0x82f63b78 : crc >> 1;
        }

        crc = ~crc;
        return ((crc >> 15) | (crc << 17)) + CrcMaskDelta;
    }
}
