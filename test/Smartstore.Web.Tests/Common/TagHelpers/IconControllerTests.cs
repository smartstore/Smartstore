using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Tests.Common.TagHelpers;

/// <summary>
/// Verifies the public icon endpoints without requiring a running store.
/// </summary>
[TestFixture]
public class IconControllerTests
{
    /// <summary>
    /// Serves exact immutable manifest revisions, and prevents caching missing revisions.
    /// </summary>
    [Test]
    public async Task Manifest_Uses_Immutable_Physical_File()
    {
        var kits = new Mock<IIconKitService>();
        var revision = new string('a', 24);
        kits.Setup(x => x.GetManifestFileAsync(revision, It.IsAny<CancellationToken>())).ReturnsAsync("C:/cache/manifest.json");
        var controller = Create(kits.Object, Mock.Of<IIconService>(), Mock.Of<IIconRenderer>());
        var result = (PhysicalFileResult)await controller.Manifest(revision, default);
        Assert.That(result.FileName, Is.EqualTo("C:/cache/manifest.json"));
        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.EntityTag.ToString(), Is.EqualTo('"' + revision + '"'));
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Does.Contain("immutable"));
        Assert.That(await controller.Manifest(new string('b', 24), default), Is.TypeOf<NotFoundResult>());
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
        icons.Setup(x => x.GetIconAsync("back?flip=x", "hi", null, It.IsAny<CancellationToken>())).ReturnsAsync(icon);
        var svg = new TagBuilder("svg");
        svg.InnerHtml.AppendHtml("<path/>");
        renderer.Setup(x => x.RenderAsync(icon, It.Is<IconOptions>(o => o.Rotate == 0 && o.FlipHorizontal == false && o.StrokeScale == 1.1), It.IsAny<CancellationToken>())).ReturnsAsync(svg);
        var controller = Create(Mock.Of<IIconKitService>(), icons.Object, renderer.Object);
        var result = (FileContentResult)await controller.Render("back?flip=x", "hi", null, 0, false, null, 1.1, default);
        Assert.That(result.ContentType, Is.EqualTo("image/svg+xml"));
        Assert.That(result.EntityTag, Is.Not.Null);
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("public,no-cache"));
        renderer.VerifyAll();
        controller.ModelState.AddModelError("rotate", "Invalid integer.");
        Assert.That(await controller.Render("back", null, null, null, null, null, null, default), Is.TypeOf<BadRequestResult>());
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
                [new IconBrowserItem("cart", "cart", "hi:cart-01@sr", "hi", "sr")]));
        var controller = new IconController(Mock.Of<IIconKitService>(), Mock.Of<IIconService>(), Mock.Of<IIconRenderer>(), browser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = (JsonResult)await controller.Browse("shared", null, null, "cart", 2);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        Assert.That(json.RootElement.GetProperty("spriteUrl").GetString(), Is.EqualTo("/shop/icons/shared-revision.svg"));
        Assert.That(json.RootElement.GetProperty("results")[0].GetProperty("id").GetString(), Is.EqualTo("cart"));
        Assert.That(json.RootElement.GetProperty("pagination").GetProperty("more").GetBoolean(), Is.False);
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
        Assert.That(await controller.Browse(null, "hi", "sr", null, 0), Is.TypeOf<BadRequestResult>());
        Assert.That(await controller.Browse(null, "hi", "sr", null, int.MaxValue), Is.TypeOf<BadRequestResult>());
        browser.VerifyAll();
    }

    /// <summary>
    /// Browser sprite responses stream exact revisions and never cache missing files.
    /// </summary>
    [Test]
    public async Task Browser_Sprite_Uses_Immutable_Physical_File()
    {
        var browser = new Mock<IIconBrowser>();
        var revision = new string('a', 24);
        browser.Setup(x => x.GetSpriteFileAsync("hi", "sr", revision, It.IsAny<CancellationToken>())).ReturnsAsync("C:/cache/browser.svg");
        var controller = new IconController(Mock.Of<IIconKitService>(), Mock.Of<IIconService>(), Mock.Of<IIconRenderer>(), browser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = (PhysicalFileResult)await controller.BrowserSprite("hi", "sr", revision, default);
        Assert.That(result.FileName, Is.EqualTo("C:/cache/browser.svg"));
        Assert.That(result.EntityTag.ToString(), Is.EqualTo('"' + revision + '"'));
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Does.Contain("immutable"));
        Assert.That(await controller.BrowserSprite("hi", "sr", new string('b', 24), default), Is.TypeOf<NotFoundResult>());
        Assert.That(controller.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
    }

    private static IconController Create(IIconKitService kits, IIconService icons, IIconRenderer renderer)
        => new(kits, icons, renderer, Mock.Of<IIconBrowser>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
