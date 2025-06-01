using System.Security.Cryptography;
using System.Text;

namespace EBookLibraryProtocol.Server
{
    static class CryptoUtils
    {
        public static (byte[] CiphertextWithIv, byte[] SymKey) EncryptBook(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] key = new byte[32]; // AES-256
            RandomNumberGenerator.Fill(key);

            byte[] cipherWithIv = EncryptAesCbc(plainBytes, key);
            return (cipherWithIv, key);
        }

        public static byte[] EncryptAesCbc(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            byte[] ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);
            return iv.Concat(ciphertext).ToArray();
        }

        public static byte[] DecryptAesCbc(byte[] cipherWithIv, byte[] key)
        {
            byte[] iv = cipherWithIv[..16];
            byte[] ciphertext = cipherWithIv[16..];

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        public static byte[] GenerateLayerKey(string bookId, string userId, int layer, int n, DateTime timestamp)
        {
            string combined = $"{bookId}{userId}{layer}{n}{timestamp:yyyyMMddHHmmss}salt";
            return SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        }

        public static string ComputeHash(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static byte[] EncryptSymmetricKeyForClient(byte[] key, RSA rsa)
        {
            return rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
        }
    }
}