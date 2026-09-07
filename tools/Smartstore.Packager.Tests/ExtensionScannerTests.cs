#nullable enable

using NUnit.Framework;
using Smartstore.Engine.Modularity;

namespace Smartstore.Packager.Tests
{
    [TestFixture]
    public class ExtensionScannerTests
    {
        [Test]
        public void Can_detect_module_below_modules_directory()
        {
            using var artifact = new TempArtifactRoot();
            artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");

            var result = new ExtensionScanner().Scan(artifact.Root);

            Assert.That(result.Failures, Is.Empty);
            Assert.That(result.Themes, Is.Empty);
            Assert.That(result.Modules.Select(x => x.Name), Is.EqualTo(new[] { "Smartstore.PayPal" }));
            Assert.That(result.Modules[0].ExtensionType, Is.EqualTo(ExtensionType.Module));
        }

        [Test]
        public void Can_detect_theme_below_themes_directory()
        {
            using var artifact = new TempArtifactRoot();
            artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");
            artifact.AddTheme("Themes/Flex", "Flex");

            var result = new ExtensionScanner().Scan(artifact.Root);

            Assert.That(result.Failures, Is.Empty);
            Assert.That(result.Themes.Select(x => x.Name), Is.EqualTo(new[] { "Flex" }));
            Assert.That(result.Themes[0].ExtensionType, Is.EqualTo(ExtensionType.Theme));
        }

        [Test]
        public void Can_detect_extensions_in_direct_subdirectories()
        {
            using var artifact = new TempArtifactRoot();
            artifact.AddModule("Smartstore.PayPal", "Smartstore.PayPal");
            artifact.AddTheme("Flex", "Flex");
            artifact.AddNonExtensionDirectory("runtimes");

            var result = new ExtensionScanner().Scan(artifact.Root);

            Assert.That(result.Failures, Is.Empty);
            Assert.That(result.Modules.Select(x => x.Name), Is.EqualTo(new[] { "Smartstore.PayPal" }));
            Assert.That(result.Themes.Select(x => x.Name), Is.EqualTo(new[] { "Flex" }));
        }

        [Test]
        public void Find_ignores_case()
        {
            using var artifact = new TempArtifactRoot();
            artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");
            artifact.AddTheme("Themes/Flex", "Flex");

            var result = new ExtensionScanner().Scan(artifact.Root);

            Assert.That(result.Find("smartstore.paypal")?.Name, Is.EqualTo("Smartstore.PayPal"));
            Assert.That(result.Find("FLEX")?.Name, Is.EqualTo("Flex"));
        }

        [Test]
        public void Find_returns_null_for_unknown_extension()
        {
            using var artifact = new TempArtifactRoot();
            artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");

            var result = new ExtensionScanner().Scan(artifact.Root);

            Assert.That(result.Find("Smartstore.DoesNotExist"), Is.Null);
        }

        [Test]
        public void Scan_throws_for_missing_root()
        {
            var scanner = new ExtensionScanner();
            var missing = Path.Combine(Path.GetTempPath(), "Smartstore.Packager.Tests", Guid.NewGuid().ToString("N"));

            Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan(missing));
        }
    }
}
