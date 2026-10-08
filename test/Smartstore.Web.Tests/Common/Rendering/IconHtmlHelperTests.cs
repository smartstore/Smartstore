using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Tests.Common.Rendering;

/// <summary>
/// Verifies HTML helper delegation, option forwarding and missing-icon behavior.
/// </summary>
[TestFixture]
public class IconHtmlHelperTests
{
    /// <summary>
    /// Forwards selectors, cancellation and caller-owned options without copying or changing them.
    /// </summary>
    [Test]
    public async Task Delegates_Rendering_Without_Mutating_Options()
    {
        using var cancellation = new CancellationTokenSource();
        var icon = new IconInfo { Name = "cart-01" };
        var svg = new TagBuilder("svg");
        var service = new Mock<IIconService>(MockBehavior.Strict);
        var renderer = new Mock<IIconRenderer>(MockBehavior.Strict);
        service.Setup(x => x.GetIconAsync("cart?rotate=90", "hi", "sr", cancellation.Token)).ReturnsAsync(icon);
        IconOptions received = null;
        renderer.Setup(x => x.RenderAsync(icon, It.IsAny<IconOptions>(), cancellation.Token))
            .Callback((IconInfo _, IconOptions options, CancellationToken _) => received = options)
            .ReturnsAsync(svg);
        using var services = new ServiceCollection().AddSingleton(service.Object).AddSingleton(renderer.Object).BuildServiceProvider();
        var helper = new Mock<IHtmlHelper>();
        helper.SetupGet(x => x.ViewContext).Returns(new ViewContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services, RequestAborted = cancellation.Token }
        });
        var options = new IconOptions { Size = "2x", Rotate = 0, FlipHorizontal = false, StrokeScale = 1.1, Animation = "spin" };
        options.Attributes["class"] = "original";
        options.Attributes["style"] = "color:red";
        options.Attributes["aria-label"] = "Cart";
        var result = await helper.Object.IconAsync("cart?rotate=90", "hi", "sr", options);
        Assert.That(result, Is.SameAs(svg));
        Assert.That(received, Is.SameAs(options));
        Assert.That(received.Size, Is.EqualTo("2x"));
        Assert.That(received.Rotate, Is.Zero);
        Assert.That(received.FlipHorizontal, Is.False);
        Assert.That(received.StrokeScale, Is.EqualTo(1.1));
        Assert.That(received.Animation, Is.EqualTo("spin"));
        Assert.That(received.Attributes["class"], Is.EqualTo("original"));
        Assert.That(received.Attributes["aria-label"], Is.EqualTo("Cart"));
        Assert.That(received.Attributes["style"], Is.EqualTo("color:red"));
    }

    /// <summary>
    /// Suppresses output when resolution or rendering returns no icon.
    /// </summary>
    /// <param name="resolved">Whether resolution succeeds before rendering returns null.</param>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Missing_Icon_Returns_Empty_Content(bool resolved)
    {
        var icon = resolved ? new IconInfo { Name = "cart-01" } : null;
        var service = new Mock<IIconService>(MockBehavior.Strict);
        var renderer = new Mock<IIconRenderer>(MockBehavior.Strict);
        service.Setup(x => x.GetIconAsync("cart", null, null, CancellationToken.None)).ReturnsAsync(icon);
        if (resolved)
        {
            renderer.Setup(x => x.RenderAsync(icon, null, CancellationToken.None)).ReturnsAsync((TagBuilder)null);
        }

        using var services = new ServiceCollection().AddSingleton(service.Object).AddSingleton(renderer.Object).BuildServiceProvider();
        var helper = new Mock<IHtmlHelper>();
        helper.SetupGet(x => x.ViewContext).Returns(new ViewContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        });
        Assert.That(await helper.Object.IconAsync("cart"), Is.SameAs(HtmlString.Empty));
        renderer.Verify(x => x.RenderAsync(It.IsAny<IconInfo>(), It.IsAny<IconOptions>(), It.IsAny<CancellationToken>()),
            resolved ? Times.Once() : Times.Never());
    }
}
