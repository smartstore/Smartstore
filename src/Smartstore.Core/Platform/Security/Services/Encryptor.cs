using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Smartstore.Core.Security
{
    public partial class Encryptor : IEncryptor
    {
        private readonly SecuritySettings _securitySettings;

        public Encryptor(SecuritySettings securitySettings)
        {
            _securitySettings = securitySettings;
        }

        public string CreateSaltKey(int size)
        {
            using var provider = new RNGCryptoServiceProvider();
            var buff = new byte[size];
            provider.GetBytes(buff);

            // Make Base64
            return Convert.ToBase64String(buff);
        }

        public string CreatePasswordHash(string password, string saltkey, string hashAlgorithm = "SHA1")
        {
            Guard.NotEmpty(hashAlgorithm, nameof(hashAlgorithm));

            var algorithm = (HashAlgorithm)CryptoConfig.CreateFromName(hashAlgorithm);
            if (algorithm == null)
                throw new ArgumentException("Unrecognized hash algorithm name.", nameof(hashAlgorithm));

            var data = Encoding.UTF8.GetBytes(string.Concat(password, saltkey));

            return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", string.Empty);
        }

        public string EncryptText(string plainText, string privateKey = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            if (string.IsNullOrEmpty(privateKey))
                privateKey = _securitySettings.EncryptionKey;

            using var provider = new TripleDESCryptoServiceProvider
            {
                Key = Encoding.ASCII.GetBytes(privateKey.Substring(0, 16)),
                IV = Encoding.ASCII.GetBytes(privateKey.Substring(8, 8))
            };

            var encryptedBinary = EncryptTextToMemory(plainText, provider.Key, provider.IV);
            return Convert.ToBase64String(encryptedBinary);
        }

        public string DecryptText(string cipherText, string privateKey = null)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            if (string.IsNullOrEmpty(privateKey))
                privateKey = _securitySettings.EncryptionKey;

            using var provider = new TripleDESCryptoServiceProvider
            {
                Key = Encoding.ASCII.GetBytes(privateKey.Substring(0, 16)),
                IV = Encoding.ASCII.GetBytes(privateKey.Substring(8, 8))
            };

            var buffer = Convert.FromBase64String(cipherText);
            return DecryptTextFromMemory(buffer, provider.Key, provider.IV);
        }

        #region Utils

        private static byte[] EncryptTextToMemory(string data, byte[] key, byte[] iv)
        {
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, new TripleDESCryptoServiceProvider().CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                var toEncrypt = Encoding.Unicode.GetBytes(data);
                cs.Write(toEncrypt, 0, toEncrypt.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }

        private static string DecryptTextFromMemory(byte[] data, byte[] key, byte[] iv)
        {
            using var ms = new MemoryStream(data);
            using (var cs = new CryptoStream(ms, new TripleDESCryptoServiceProvider().CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                using var sr = new StreamReader(cs, Encoding.Unicode);
                return sr.ReadLine();
            }
        }

        #endregion
    }
}