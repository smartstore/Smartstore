using System;
using NUnit.Framework;
using Smartstore.Core.Security;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Security
{
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
    }
}
