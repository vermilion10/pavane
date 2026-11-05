using System.Security.Cryptography;
using System.Text;

namespace Pavane.Common;

// the master-data ZIP password is carried in /public/assets as `encrypted_password`.
// AssetDownloadService.GetMasterDataPwd reads it as `base64(iv[16] || AES-256-CBC-PKCS7(UTF8(password), key))` using the same CommonKey as every other Takasho route, then reads the plaintext as a string.
// the key is app-embedded (not session-bound), so Pavane can both forge a value for a known password and recover the password from any genuine captured value.
public static class MasterDataPwd
{
    private const int IvSize = 16;

    // builds the `encrypted_password` value the client decodes back to `password`
    public static string Encode(string password)
    {
        var iv = RandomNumberGenerator.GetBytes(IvSize);
        using var aes = Aes.Create();
        aes.Key = Packer.Key;
        var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(password), iv, PaddingMode.PKCS7);
        return Convert.ToBase64String([.. iv, .. cipher]);
    }

    // recovers the plaintext ZIP password from a captured `encrypted_password` value.
    public static string Decode(string encryptedPassword)
    {
        var raw = Convert.FromBase64String(encryptedPassword.Trim());
        if (raw.Length <= IvSize)
            throw new ArgumentException("value is too short to hold a 16-byte IV plus ciphertext", nameof(encryptedPassword));

        var iv = raw.AsSpan(0, IvSize).ToArray();
        using var aes = Aes.Create();
        aes.Key = Packer.Key;
        var plain = aes.DecryptCbc(raw.AsSpan(IvSize), iv, PaddingMode.PKCS7);
        return Encoding.UTF8.GetString(plain);
    }
}
