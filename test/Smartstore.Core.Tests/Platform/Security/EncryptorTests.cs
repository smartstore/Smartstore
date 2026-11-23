using System;
using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using Smartstore.Core.Security;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Security;

[TestFixture]
public class EncryptorTests
{
    IEncryptor _encryptor;
    SecuritySettings _securitySettings;

    [OneTimeSetUp]
    public void SetUp()
    {
        _securitySettings = new SecuritySettings()
        {
            EncryptionKey = "273ece6f97dd844d"
        };

        _encryptor = new Encryptor(_securitySettings);
    }

    [Test]
    public void Can_hash()
    {
        string password = "MyLittleSecret";
        var saltKey = "salt1";
        var hashedPassword = _encryptor.CreatePasswordHash(password, saltKey);
        //hashedPassword.ShouldBeNotBeTheSameAs(password);
        hashedPassword.ShouldEqual("A07A9638CCE93E48E3F26B37EF7BDF979B8124D6");
    }

    [Test]
    public void Can_encrypt_and_decrypt()
    {
        var password = "MyLittleSecret";
        string encryptedPassword = _encryptor.EncryptText(password);
        var decryptedPassword = _encryptor.DecryptText(encryptedPassword);
        decryptedPassword.ShouldEqual(password);
    }

    [Test]
    public void Password_hasher_options_are_deferred_and_applied_once()
    {
        var encryptor = new CustomEncryptor(_securitySettings);
        encryptor.CreatePasswordHash("secret", "salt");
        Assert.That(encryptor.OptionsCalls, Is.Zero);

        string first = encryptor.CreatePasswordHash("secret", null, "PBKDF2");
        string second = encryptor.CreatePasswordHash("secret", null, "PBKDF2");

        Assert.That(encryptor.VerifyPasswordHash(first, "secret", null),
            Is.EqualTo(PasswordVerificationResult.Success));
        Assert.That(encryptor.VerifyPasswordHash(first, "wrong", null),
            Is.EqualTo(PasswordVerificationResult.Failed));
        Assert.That(encryptor.OptionsCalls, Is.EqualTo(1));
        Assert.That(first, Does.StartWith("AQ"));
        Assert.That(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(
            Convert.FromBase64String(first).AsSpan(5)), Is.EqualTo(230_000));
        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void New_hash_uses_identity_v3_and_a_fresh_salt()
    {
        string hash = _encryptor.CreatePasswordHash("secret", null, "PBKDF2");
        byte[] bytes = Convert.FromBase64String(hash);

        Assert.That(hash, Does.StartWith("AQ"));
        Assert.That(hash.Length, Is.LessThanOrEqualTo(500));
        Assert.That(bytes[0], Is.EqualTo(1));
        Assert.That(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1)), Is.EqualTo(2));
        Assert.That(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5)), Is.EqualTo(220_000));
        Assert.That(_encryptor.CreatePasswordHash("secret", null, "PBKDF2"), Is.Not.EqualTo(hash));
        Assert.That(_encryptor.VerifyPasswordHash(hash, "secret", null), Is.EqualTo(PasswordVerificationResult.Success));
    }

    [TestCase("AQ!")]
    [TestCase("AQ==")]
    [TestCase("AQAAAA==")]
    public void Malformed_identity_hash_fails(string hash)
    {
        Assert.That(_encryptor.VerifyPasswordHash(hash, "secret", null),
            Is.EqualTo(PasswordVerificationResult.Failed));
    }

    private sealed class CustomEncryptor : Encryptor
    {
        public CustomEncryptor(SecuritySettings settings)
            : base(settings)
        {
        }

        public int OptionsCalls { get; private set; }

        protected override PasswordHasherOptions CreatePasswordHasherOptions()
        {
            OptionsCalls++;
            var options = base.CreatePasswordHasherOptions();
            options.IterationCount = 230_000;
            return options;
        }
    }

}