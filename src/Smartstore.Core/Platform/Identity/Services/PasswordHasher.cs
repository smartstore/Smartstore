using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Core.Identity;

public class PasswordHasher : IPasswordHasher<Customer>
{
    private readonly IEncryptor _encryptor;
    private readonly CustomerSettings _customerSettings;

    public PasswordHasher(IEncryptor encryptor, CustomerSettings customerSettings)
    {
        _encryptor = encryptor;
        _customerSettings = customerSettings;
    }

    public string HashPassword(Customer user, string password)
    {
        Guard.NotNull(password);

        switch (user.PasswordFormat)
        {
            case PasswordFormat.Hashed:
                return _encryptor.CreatePasswordHash(password, null, Encryptor.PBKDF2Algorithm);
            case PasswordFormat.Encrypted:
                return _encryptor.EncryptText(password);
            default: // Clear
                return password;
        }
    }

    public PasswordVerificationResult VerifyHashedPassword(Customer user, string hashedPassword, string providedPassword)
    {
        Guard.NotNull(hashedPassword);
        Guard.NotNull(providedPassword);

        string pwd = providedPassword;
        switch (user.PasswordFormat)
        {
            case PasswordFormat.Encrypted:
                pwd = _encryptor.EncryptText(providedPassword);
                break;
            case PasswordFormat.Hashed:
                return _encryptor.VerifyPasswordHash(
                    hashedPassword,
                    providedPassword,
                    user.PasswordSalt,
                    _customerSettings.HashedPasswordFormat);
        }

        if (pwd.Length > 0 && pwd == hashedPassword)
        {
            return PasswordVerificationResult.Success;
        }

        return PasswordVerificationResult.Failed;
    }
}