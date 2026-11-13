using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Smartstore.IconKit;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Tests.Common.Rendering;

/// <summary>
/// Verifies expression delegation and empty rendering output.
/// </summary>
[TestFixture]
public class IconHtmlHelperTests
{
    /// <summary>
    /// Both entry points preserve the expression and optional render context without an explicit cancellation token.
    /// </summary>
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Delegates_Expression_And_Optional_Render_Options(bool fromString, bool withOptions)
    {
        using var cancellation = new CancellationTokenSource();
        var expression = IconExpression.Parse("hi:cart!/sr?size=2x&rotate=0&flip-h=false&stroke-scale=1.1&animation=spin");
        var options = withOptions ? new IconOptions
        {
            Area = "admin",
            ForceInline = true,
            Attributes = { ["class"] = "custom", ["aria-label"] = "Cart" }
        } : null;
        var svg = new TagBuilder("svg");
        var renderer = new Mock<IIconRenderer>(MockBehavior.Strict);
        renderer.Setup(x => x.RenderAsync(expression, options, CancellationToken.None)).ReturnsAsync(svg);
        using var services = new ServiceCollection().AddSingleton(renderer.Object).BuildServiceProvider();
        var helper = new Mock<IHtmlHelper>();
        helper.SetupGet(x => x.ViewContext).Returns(new ViewContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services, RequestAborted = cancellation.Token }
        });

        var result = fromString
            ? await helper.Object.IconAsync(expression.ToString(), options)
            : await helper.Object.IconAsync(expression, options);

        Assert.That(result, Is.SameAs(svg));
        renderer.VerifyAll();
    }

    /// <summary>
    /// Suppresses output when the renderer returns no icon, including an unavailable fallback.
    /// </summary>
    [Test]
    public async Task Missing_Icon_Returns_Empty_Content()
    {
        var renderer = new Mock<IIconRenderer>(MockBehavior.Strict);
        renderer.Setup(x => x.RenderAsync(IconExpression.Parse("cart"), null, CancellationToken.None))
            .ReturnsAsync((TagBuilder)null);
        using var services = new ServiceCollection().AddSingleton(renderer.Object).BuildServiceProvider();
        var helper = new Mock<IHtmlHelper>();
        helper.SetupGet(x => x.ViewContext).Returns(new ViewContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services }
        });

        Assert.That(await helper.Object.IconAsync("cart"), Is.SameAs(HtmlString.Empty));
        renderer.VerifyAll();
    }
}
