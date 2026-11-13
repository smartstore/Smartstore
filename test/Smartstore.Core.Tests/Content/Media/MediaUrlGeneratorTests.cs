using NUnit.Framework;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Tests.Content.Media;

[TestFixture]
public class MediaUrlGeneratorTests
{
    [TestCase("default-image.png")]
    [TestCase("DEFAULT-IMAGE.PNG")]
    [TestCase("/default-image.png")]
    public void Fallback_image_path_is_accepted(string path)
    {
        Assert.That(MediaUrlGenerator.IsFallbackImagePath(path, "default-image.png"), Is.True);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("other-image.png")]
    [TestCase("../../App_Data/Settings.txt")]
    [TestCase(@"..\..\App_Data\Settings.txt")]
    [TestCase("../default-image.png")]
    [TestCase(@"..\default-image.png")]
    [TestCase("sub/default-image.png")]
    public void Any_other_path_is_rejected(string path)
    {
        Assert.That(MediaUrlGenerator.IsFallbackImagePath(path, "default-image.png"), Is.False);
    }

    [TestCase("")]
    [TestCase(null)]
    public void Unconfigured_fallback_image_rejects_everything(string fallbackImageFileName)
    {
        Assert.That(MediaUrlGenerator.IsFallbackImagePath("default-image.png", fallbackImageFileName), Is.False);
    }

    [Test]
    public void Fallback_image_may_reside_in_a_subfolder()
    {
        Assert.That(MediaUrlGenerator.IsFallbackImagePath("sub/placeholder.png", "sub/placeholder.png"), Is.True);
        Assert.That(MediaUrlGenerator.IsFallbackImagePath(@"sub\placeholder.png", "sub/placeholder.png"), Is.True);
        Assert.That(MediaUrlGenerator.IsFallbackImagePath("sub/placeholder.png", @"sub\placeholder.png"), Is.True);
        Assert.That(MediaUrlGenerator.IsFallbackImagePath("placeholder.png", "sub/placeholder.png"), Is.False);
    }

    [TestCase("default-image.png", "default-image.png")]
    [TestCase("  default-image.png  ", "default-image.png")]
    [TestCase("~/default-image.png", "default-image.png")]
    [TestCase("/~default-image.png", "default-image.png")]
    [TestCase("/default-image.png/", "default-image.png")]
    [TestCase(@"\placeholder.png", "placeholder.png")]
    [TestCase(null, "")]
    public void Fallback_image_file_name_is_normalized(string fileName, string expected)
    {
        Assert.That(MediaUrlGenerator.NormalizeFallbackImageFileName(fileName).ToString(), Is.EqualTo(expected));
    }

    [TestCase("default-image.png")]
    [TestCase(" /~default-image.png ")]
    [TestCase("/~default-image.png")]
    [TestCase(@"~\sub\placeholder.png")]
    public void Normalization_is_idempotent(string fileName)
    {
        var once = MediaUrlGenerator.NormalizeFallbackImageFileName(fileName).ToString();
        var twice = MediaUrlGenerator.NormalizeFallbackImageFileName(once).ToString();

        Assert.That(twice, Is.EqualTo(once));
    }

    [Test]
    public void Default_image_name_is_loadable_but_never_persisted()
    {
        // SettingFactory loads every setting property that has a setter, but persists publicly settable
        // ones only. The private setter is what keeps this constant out of the setting table while a
        // manually stored "Media.DefaultImageName" still overrides it. Do not make the setter public.
        var property = typeof(MediaSettings).GetProperty(nameof(MediaSettings.DefaultImageName));

        Assert.That(property, Is.Not.Null);
        Assert.That(property.CanWrite, Is.True, "A stored setting must still override the default.");
        Assert.That(property.GetSetMethod(false), Is.Null, "A public setter would persist the value on every settings save.");
    }

    [TestCase("default-image.png")]
    [TestCase("/~default-image.png")]
    [TestCase("  default-image.png  ")]
    [TestCase("sub/placeholder.png")]
    [TestCase(@"sub\placeholder.png")]
    public void Generated_fallback_path_is_accepted_again(string configuredFileName)
    {
        // What GenerateUrl() puts into the URL is the normalized name. It must pass the check
        // that MediaMiddleware performs against the configured (raw) setting value.
        var emitted = MediaUrlGenerator.NormalizeFallbackImageFileName(configuredFileName).ToString();

        Assert.That(MediaUrlGenerator.IsFallbackImagePath(emitted, configuredFileName), Is.True);
    }
}
