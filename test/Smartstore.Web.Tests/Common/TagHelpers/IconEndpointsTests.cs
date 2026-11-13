using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using NUnit.Framework;
using Smartstore.IconKit;

namespace Smartstore.Web.Tests.Common.TagHelpers;

/// <summary>
/// Verifies the public icon endpoints without requiring a running store.
/// </summary>
[TestFixture]
public class IconEndpointsTests
{
    /// <summary>
    /// Serves exact immutable manifest revisions, and prevents caching missing revisions.
    /// </summary>
    [Test]
    public async Task Manifest_Uses_Immutable_Physical_File()
    {
        var kits = new Mock<IIconKitService>();
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "cache", "manifest.json");
        var revision = new string('a', 24);
        kits.Setup(x => x.GetManifestFileAsync(revision, It.IsAny<CancellationToken>())).ReturnsAsync(path);
        var controller = Create(kits.Object, Mock.Of<IIconService>(), Mock.Of<IIconRenderer>());
        var result = (PhysicalFileHttpResult)await controller.Manifest(revision, default);
        Assert.That(result.FileName, Is.EqualTo(path));
        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.EntityTag.ToString(), Is.EqualTo('"' + revision + '"'));
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Does.Contain("immutable"));
        Assert.That(await controller.Manifest(new string('b', 24), default), Is.TypeOf<NotFound>());
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
    }

    /// <summary>
    /// Preserves explicit false and zero overrides and returns SVG with a revalidation ETag.
    /// </summary>
    [Test]
    public async Task Render_Delegates_Modifiers_And_Returns_Svg()
    {
        var icons = new Mock<IIconService>();
        var renderer = new Mock<IIconRenderer>();
        var icon = new IconInfo { Name = "arrow" };
        icons.Setup(x => x.GetIconAsync(new IconAddress("back", "hi", null), "public", It.IsAny<CancellationToken>())).ReturnsAsync(icon);
        var svg = new TagBuilder("svg");
        svg.InnerHtml.AppendHtml("<path/>");
        renderer.Setup(x => x.RenderAsync(icon, It.Is<IconPresentation>(p => p.Rotate == 0 && p.FlipHorizontal == false && p.StrokeScale == 1.1), It.Is<IconOptions>(o => o.RequestedName == "back?flip=x"), It.IsAny<CancellationToken>())).ReturnsAsync(svg);
        var controller = Create(Mock.Of<IIconKitService>(), icons.Object, renderer.Object);
        var result = (FileContentHttpResult)await controller.Render("back?flip=x", "hi", null, 0, false, null, 1.1, "public", default);
        Assert.That(result.ContentType, Is.EqualTo("image/svg+xml"));
        Assert.That(result.EntityTag, Is.Not.Null);
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("public,no-cache"));
        renderer.VerifyAll();
        Assert.That(await controller.Render(string.Empty, null, null, null, null, null, null, "public", default), Is.TypeOf<BadRequest>());
    }

    /// <summary>
    /// Binds page boundaries and returns Select2 metadata without duplicating the shared sprite URL per row.
    /// </summary>
    [Test]
    public async Task Browser_Search_Returns_Select2_Page_And_Rejects_Invalid_Pagination()
    {
        var browser = new Mock<IIconBrowser>();
        browser.Setup(x => x.SearchAsync(It.Is<IconSearchQuery>(q => q.Skip == 50 && q.Take == 50 && q.Term == "cart"),
            "shared", It.IsAny<CancellationToken>())).ReturnsAsync(new IconBrowserResult("/shop/icons/shared-revision.svg", 51,
                [new IconBrowserItem("cart", "cart", "hi:cart-01/sr", "hi", "sr", IsExternal: true)]));
        var controller = new IconEndpoints(new DefaultHttpContext(), Mock.Of<IIconKitService>(), Mock.Of<IIconService>(), Mock.Of<IIconRenderer>(), browser.Object);
        var result = (IValueHttpResult)await controller.Browse("shared", null, null, "cart", 2);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.That(json.RootElement.GetProperty("spriteUrl").GetString(), Is.EqualTo("/shop/icons/shared-revision.svg"));
        Assert.That(json.RootElement.GetProperty("results")[0].GetProperty("id").GetString(), Is.EqualTo("cart"));
        Assert.That(json.RootElement.GetProperty("external")[0].GetInt32(), Is.Zero);
        Assert.That(json.RootElement.GetProperty("pagination").GetProperty("more").GetBoolean(), Is.False);
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
        Assert.That(await controller.Browse(null, "hi", "sr", null, 0), Is.TypeOf<BadRequest>());
        Assert.That(await controller.Browse(null, "hi", "sr", null, int.MaxValue), Is.TypeOf<BadRequest>());
        browser.VerifyAll();
    }

    /// <summary>
    /// Browser sprite responses stream exact revisions and never cache missing files.
    /// </summary>
    [Test]
    public async Task Browser_Sprite_Uses_Immutable_Physical_File()
    {
        var browser = new Mock<IIconBrowser>();
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "cache", "browser.svg");
        var revision = new string('a', 24);
        browser.Setup(x => x.GetSpriteFileAsync("hi", "sr", revision, It.IsAny<CancellationToken>())).ReturnsAsync(path);
        var controller = new IconEndpoints(new DefaultHttpContext(), Mock.Of<IIconKitService>(), Mock.Of<IIconService>(), Mock.Of<IIconRenderer>(), browser.Object);
        var result = (PhysicalFileHttpResult)await controller.BrowserSprite("hi", "sr", revision, default);
        Assert.That(result.FileName, Is.EqualTo(path));
        Assert.That(result.EntityTag.ToString(), Is.EqualTo('"' + revision + '"'));
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Does.Contain("immutable"));
        Assert.That(await controller.BrowserSprite("hi", "sr", new string('b', 24), default), Is.TypeOf<NotFound>());
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
    }

    private static IconEndpoints Create(IIconKitService kits, IIconService icons, IIconRenderer renderer)
        => new(new DefaultHttpContext(), kits, icons, renderer, Mock.Of<IIconBrowser>());
}
