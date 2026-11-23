using Microsoft.AspNetCore.Identity;

#nullable enable

namespace Smartstore.Core.Security;

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
    /// Creates a password hash using the selected algorithm.
    /// </summary>
    /// <param name="password">Password</param>
    /// <param name="saltkey">Salt key for legacy algorithms. Ignored for PBKDF2, which generates and embeds its own salt.</param>
    /// <param name="hashAlgorithm">A known hash algorithm, or PBKDF2 for an ASP.NET Core Identity V3 password hash.</param>
    /// <remarks>
    /// SHA1 remains the default for backward compatibility. Select PBKDF2 for new passwords;
    /// it uses HMAC-SHA512 with 220,000 iterations and embeds a fresh salt and its parameters.
    /// Verify the result with <see cref="VerifyPasswordHash"/> instead of comparing newly generated hashes.
    /// </remarks>
    /// <returns>An Identity V3 payload for PBKDF2, or a hexadecimal hash for a legacy algorithm.</returns>
    string CreatePasswordHash(string password, string? saltkey, string hashAlgorithm = "SHA1");

    /// <summary>
    /// Verifies an Identity V3 password hash or a legacy hexadecimal password hash.
    /// </summary>
    /// <param name="hashedPassword">The stored password hash.</param>
    /// <param name="providedPassword">The password to verify.</param>
    /// <param name="saltkey">The stored legacy salt. Ignored for Identity V3 hashes.</param>
    /// <param name="hashAlgorithm">The legacy algorithm; ignored for Identity V3 hashes.</param>
    /// <returns>
    /// The verification result, including SuccessRehashNeeded for valid hashes that require an upgrade.
    /// </returns>
    /// <remarks>
    /// On SuccessRehashNeeded, create and persist a new PBKDF2 hash from the provided password.
    /// </remarks>
    PasswordVerificationResult VerifyPasswordHash(
        string hashedPassword,
        string providedPassword,
        string? saltkey,
        string hashAlgorithm = "SHA1");

    /// <summary>
    /// Encrypts text
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="privateKey">Encryption private key</param>
    /// <returns>Encrypted text</returns>
    string EncryptText(string plainText, string? privateKey = null);

    /// <summary>
    /// Decrypts text
    /// </summary>
    /// <param name="cipherText">Text to decrypt</param>
    /// <param name="privateKey">Encryption private key</param>
    /// <returns>Decrypted text</returns>
    string DecryptText(string cipherText, string? privateKey = null);
}