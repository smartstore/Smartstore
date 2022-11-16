namespace Smartstore.Core.Security
{
    /// <summary>
    /// Responsible for hashing passwords, encrypting or decrypting text and generating salt keys.
    /// </summary>
    public interface IEncryptor
    {
        /// <summary>
        /// Creates salt key
        /// </summary>
        /// <param name="size">Key size</param>
        /// <returns>Salt key</returns>
        string CreateSaltKey(int size);

        /// <summary>
        /// Creates a password hash
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="saltkey">Salk key</param>
        /// <param name="hashAlgorithm">A known hash algorithm</param>
        /// <returns>Password hash</returns>
        string CreatePasswordHash(string password, string saltkey, string hashAlgorithm = "SHA1");

        /// <summary>
        /// Encrypts text
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="privateKey">Encryption private key</param>
        /// <returns>Encrypted text</returns>
        string EncryptText(string plainText, string privateKey = null);

        /// <summary>
        /// Decrypts text
        /// </summary>
        /// <param name="cipherText">Text to decrypt</param>
        /// <param name="privateKey">Encryption private key</param>
        /// <returns>Decrypted text</returns>
        string DecryptText(string cipherText, string privateKey = null);
    }
}