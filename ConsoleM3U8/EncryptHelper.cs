using System.Security.Cryptography;
using System.Text;

namespace ConsoleM3U8
{
    public static class EncryptHelper
    {

        public static byte[] GetRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(bytes);
            return bytes;
        }

        public static async Task EncryptFileAsync(string path, byte[] key, byte[] iv, string outFile)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(path);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] encrypted = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
            await File.WriteAllBytesAsync(outFile, encrypted);
        }

        public static string ByteArrayToHexString(byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

    }
}