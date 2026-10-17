using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Installation;
using Smartstore.Core.Tests.Platform.Installation.SeedSnapshot;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Core.Tests.Platform.Installation;

/// <summary>
/// Reads the installer and app languages from the real "App_Data/Seed" folder.
/// </summary>
[TestFixture]
public class InstallationServiceLanguageTests
{
    private static InstallationService CreateService()
    {
        var appDataDir = Path.Combine(InstallationSeedSnapshotTests.FindRepositoryRoot(), "src", "Smartstore.Web", "App_Data");

        var appContext = new Mock<IApplicationContext>();
        appContext.SetupGet(x => x.AppDataRoot).Returns(new LocalFileSystem(appDataDir));

        return new InstallationService(null, null, appContext.Object, null, null);
    }

    [Test]
    public void Installation_languages_come_from_culture_folders()
    {
        var languages = CreateService().GetInstallationLanguages();

        Assert.That(languages.Select(x => x.Code), Is.EquivalentTo(new[] { "de", "en" }));

        var en = languages.Single(x => x.Code == "en");
        Assert.That(en.IsDefault, Is.True);
        Assert.That(en.Resources["AdminEmail"].Value, Is.EqualTo("Admin user email"));

        var de = languages.Single(x => x.Code == "de");
        Assert.That(de.Name, Is.EqualTo("Deutsch"));
        Assert.That(de.Resources["AdminEmail"].Value, Is.EqualTo("Administrator E-Mail"));
    }

    [Test]
    public void App_languages_come_from_culture_files()
    {
        var service = CreateService();

        Assert.That(service.GetAppLanguages().Select(x => x.Culture), Is.EquivalentTo(new[] { "de-DE", "en-US" }));
        Assert.That(service.GetAppLanguage("de-de").UniqueSeoCode, Is.EqualTo("de"));
        Assert.That(service.GetAppLanguage("fr-FR"), Is.Null);
    }
}
