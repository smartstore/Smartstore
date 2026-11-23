using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using PasswordHasher = Smartstore.Core.Identity.PasswordHasher;

namespace Smartstore.Core.Tests.Identity;

[TestFixture]
public class PasswordHasherTests
{
    private readonly Encryptor _encryptor = new(new SecuritySettings());

    [TestCase("SHA1")]
    [TestCase("SHA256")]
    [TestCase("SHA512")]
    public async Task Legacy_password_is_upgraded_by_user_manager(string algorithm)
    {
        var user = new Customer { PasswordFormat = PasswordFormat.Hashed, PasswordSalt = "salt1" };
        user.Password = _encryptor.CreatePasswordHash("secret", user.PasswordSalt, algorithm);
        var hasher = new PasswordHasher(_encryptor, new CustomerSettings { HashedPasswordFormat = algorithm });
        var store = new Mock<IUserPasswordStore<Customer>>();
        store.Setup(x => x.GetPasswordHashAsync(user, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(user.Password));
        store.Setup(x => x.SetPasswordHashAsync(user, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, string, CancellationToken>((customer, hash, _) => customer.Password = hash)
            .Returns(Task.CompletedTask);
        store.Setup(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        using var manager = new UserManager<Customer>(store.Object,
            Options.Create(new IdentityOptions()), hasher,
            Array.Empty<IUserValidator<Customer>>(), Array.Empty<IPasswordValidator<Customer>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null,
            NullLogger<UserManager<Customer>>.Instance);

        Assert.That(await manager.CheckPasswordAsync(user, "wrong"), Is.False);
        store.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Never);

        Assert.That(await manager.CheckPasswordAsync(user, "secret"), Is.True);
        Assert.That(user.Password, Does.StartWith("AQ"));
        store.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(hasher.VerifyHashedPassword(user, user.Password, "secret"),
            Is.EqualTo(PasswordVerificationResult.Success));
        Assert.That(hasher.VerifyHashedPassword(user, user.Password, "wrong"),
            Is.EqualTo(PasswordVerificationResult.Failed));
    }

    [TestCase(PasswordFormat.Clear)]
    [TestCase(PasswordFormat.Encrypted)]
    public void Other_password_formats_keep_their_behavior(PasswordFormat format)
    {
        var hasher = new PasswordHasher(_encryptor, new CustomerSettings());
        var user = new Customer { PasswordFormat = format };
        string hash = hasher.HashPassword(user, "secret");
        Assert.That(hasher.VerifyHashedPassword(user, hash, "secret"),
            Is.EqualTo(PasswordVerificationResult.Success));
        Assert.That(hasher.VerifyHashedPassword(user, hash, "wrong"),
            Is.EqualTo(PasswordVerificationResult.Failed));
    }
}
