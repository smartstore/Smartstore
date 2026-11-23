using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Smartstore.Core.Security;

public partial class Encryptor : IEncryptor
{
    /// <summary>
    /// Algorithm name for creating Identity V3 password hashes.
    /// </summary>
    internal const string PBKDF2Algorithm = "PBKDF2";

    /// <summary>
    /// Base64 prefix distinguishing supported Identity V3 hashes from legacy hexadecimal hashes.
    /// </summary>
    private const string IdentityV3Prefix = "AQ";

    private readonly Lazy<PasswordHasher<object>> _passwordHasher;
    private readonly SecuritySettings _securitySettings;

    public Encryptor(SecuritySettings securitySettings)
    {
        _securitySettings = securitySettings;
        _passwordHasher = new Lazy<PasswordHasher<object>>(() =>
            new PasswordHasher<object>(Options.Create(CreatePasswordHasherOptions())));
    }

    public string CreateSaltKey(int size)
    {
        using var provider = RandomNumberGenerator.Create();
        var buff = new byte[size];
        provider.GetBytes(buff);

        // Make Base64
        return Convert.ToBase64String(buff);
    }

    public string CreatePasswordHash(string password, string saltkey, string hashAlgorithm = "SHA1")
    {
        Guard.NotEmpty(hashAlgorithm);

        if (hashAlgorithm.Equals(PBKDF2Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            return _passwordHasher.Value.HashPassword(null, password);
        }

        var algorithm = (HashAlgorithm)CryptoConfig.CreateFromName(hashAlgorithm);
        if (algorithm == null)
            throw new ArgumentException("Unrecognized hash algorithm name.", nameof(hashAlgorithm));

        var data = string.Concat(password, saltkey).GetBytes();

        return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", string.Empty);
    }

    /// <inheritdoc/>
    public PasswordVerificationResult VerifyPasswordHash(
        string hashedPassword,
        string providedPassword,
        string saltkey,
        string hashAlgorithm = "SHA1")
    {
        Guard.NotNull(hashedPassword);
        Guard.NotNull(providedPassword);
        Guard.NotEmpty(hashAlgorithm);

        // Identity V3 starts with "AQ" in Base64; legacy hexadecimal hashes cannot.
        if (hashedPassword.StartsWith(IdentityV3Prefix, StringComparison.Ordinal))
        {
            try
            {
                return _passwordHasher.Value.VerifyHashedPassword(null, hashedPassword, providedPassword);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        if (hashAlgorithm.Equals(PBKDF2Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            return PasswordVerificationResult.Failed;
        }

        string legacyHash = CreatePasswordHash(providedPassword, saltkey, hashAlgorithm);
        return legacyHash.Length > 0 && legacyHash == hashedPassword
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Failed;
    }

    public string EncryptText(string plainText, string privateKey = null)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (string.IsNullOrEmpty(privateKey))
            privateKey = _securitySettings.EncryptionKey;

        var encryptedBinary = EncryptTextToMemory(plainText, privateKey);
        return Convert.ToBase64String(encryptedBinary);
    }

    public string DecryptText(string cipherText, string privateKey = null)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (string.IsNullOrEmpty(privateKey))
            privateKey = _securitySettings.EncryptionKey;

        var buffer = Convert.FromBase64String(cipherText);
        return DecryptTextFromMemory(buffer, privateKey);
    }

    #region Utils

    /// <summary>
    /// Creates the options once, when the password hasher is first needed.
    /// </summary>
    /// <remarks>
    /// Overrides must retain IdentityV3 compatibility for format detection.
    /// </remarks>
    protected virtual PasswordHasherOptions CreatePasswordHasherOptions()
    {
        return new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = 220_000
        };
    }

    private static SymmetricAlgorithm CreateAlgorithm()
    {
        var algo = TripleDES.Create();
        algo.Padding = PaddingMode.PKCS7;
        return algo;
    }

    private static byte[] EncryptTextToMemory(string data, string privateKey)
    {
        var key = Encoding.ASCII.GetBytes(privateKey[..16]);
        var iv = Encoding.ASCII.GetBytes(privateKey.Substring(8, 8));

        using var algo = CreateAlgorithm();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, algo.CreateEncryptor(key, iv), CryptoStreamMode.Write))
        {
            var toEncrypt = Encoding.Unicode.GetBytes(data);
            cs.Write(toEncrypt, 0, toEncrypt.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    private static string DecryptTextFromMemory(byte[] data, string privateKey)
    {
        var key = Encoding.ASCII.GetBytes(privateKey[..16]);
        var iv = Encoding.ASCII.GetBytes(privateKey.Substring(8, 8));

        using var algo = CreateAlgorithm();
        using var ms = new MemoryStream(data);
        using (var cs = new CryptoStream(ms, algo.CreateDecryptor(key, iv), CryptoStreamMode.Read))
        {
            using var sr = new StreamReader(cs, Encoding.Unicode);
            return sr.ReadLine();
        }
    }

    #endregion
}