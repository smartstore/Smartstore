using System;
using Microsoft.Extensions.FileProviders;
using Moq;
using NUnit.Framework;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Tests.Platform.Modularity;

[TestFixture]
public class ModuleIconTests
{
    [TestCase("icon-P.svg,icon-P.png,icon.svg,icon.png", "cart", "~/Modules/Test/icon-P.svg", null)]
    [TestCase("icon-P.png,icon.svg", "cart", "~/Modules/Test/icon-P.png", null)]
    [TestCase("icon.svg,icon.png", "cart", null, "cart")]
    [TestCase("icon.svg,icon.png", "cart?color=#123456", null, "cart?color=#123456")]
    [TestCase("icon.svg,icon.png", null, "~/Modules/Test/icon.svg", null)]
    [TestCase("icon.png", null, "~/Modules/Test/icon.png", null)]
    [TestCase("", null, null, "hi:plug-02")]
    public void Resolves_Provider_Manifest_And_Image_Priority(string files, string address, string imageUrl, string expectedAddress)
    {
        var descriptor = CreateDescriptor(files, address);
        var manager = new ModuleManager(null, null, null, null, null, null, null, null);

        var icon = manager.GetIcon(descriptor, "P");

        Assert.That(icon.ImageUrl, Is.EqualTo(imageUrl));
        Assert.That(icon.Address, Is.EqualTo(expectedAddress));
    }

    [Test]
    public void Legacy_Url_Api_Still_Returns_An_Image()
    {
        var manager = new ModuleManager(null, null, null, null, null, null, null, null);
        var descriptor = CreateDescriptor("icon.svg,icon.png", "cart");

        Assert.That(manager.GetIconUrl(descriptor), Is.EqualTo("~/Modules/Test/icon.svg"));
        Assert.That(manager.GetIcon(descriptor).Address, Is.EqualTo("cart"));
    }

    [Test]
    public void Manifest_Parses_Optional_Icon_Properties()
    {
        var descriptor = ModuleDescriptor.ParseManifest("""
            { "SystemName": "Test", "FriendlyName": "Test", "Version": "1.0", "Icon": "hi:puzzle/sr?color=#123456" }
            """);
        var legacy = ModuleDescriptor.ParseManifest("""
            { "SystemName": "Test", "FriendlyName": "Test", "Version": "1.0" }
            """);

        Assert.That(descriptor.Icon, Is.EqualTo("hi:puzzle/sr?color=#123456"));
        Assert.That(legacy.Icon, Is.Null);
    }

    private static IModuleDescriptor CreateDescriptor(string files, string address)
    {
        var fileProvider = new Mock<IFileProvider>();
        fileProvider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string name) =>
            Mock.Of<IFileInfo>(x => x.Exists == Array.Exists(files.Split(','), file => file == name)));
        var descriptor = new Mock<IModuleDescriptor>();
        descriptor.SetupGet(x => x.WebRoot).Returns(fileProvider.Object);
        descriptor.SetupGet(x => x.Path).Returns("/Modules/Test/");
        descriptor.SetupGet(x => x.Group).Returns("Admin");
        descriptor.SetupGet(x => x.Icon).Returns(address);
        return descriptor.Object;
    }
}
