using System;
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

    private static IconController Create(IIconKitService kits, IIconService icons, IIconRenderer renderer)
        => new(kits, icons, renderer)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
