using System.IO;
using NUnit.Framework;

namespace Smartstore.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        private Stream GetFileStream(string fileName)
        {
            return typeof(ExtensionsTests).Assembly.GetManifestResourceStream("Smartstore.Tests.Files.{0}".FormatInvariant(fileName));
        }

        [Test]
        public void Can_Strip_Html()
        {
            var html = GetFileStream("testdata.html").AsString();
            var text = html.RemoveHtml();

            Assert.That(text, Does.StartWith("Produktmerkmale"), "Produktmerkmale");
            Assert.That(text, Does.Not.Contain("function()"), "No function()");
            Assert.That(text, Does.Not.Contain(".someclass"), "No .someclass");
            Assert.That(text, Does.Not.Contain("This is a comment and should be stripped from result"), "No comment");
            Assert.That(text, Does.EndWith("Technologie:WCDM"), "EndsWith Technologie:WCDM");
        }
    }
}