using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Tests.Common.TagHelpers;

/// <summary>
/// Verifies the minimal SVG helper's rendering, cache isolation and missing-icon behavior.
/// </summary>
[TestFixture]
public class IconTagHelperTests
{
    /// <summary>
    /// Skips resolution when the cheatsheet selects another library through sm-if.
    /// </summary>
    [Test]
    public async Task Suppressed_Icon_Does_Not_Resolve_Svg()
    {
        var service = new Mock<IIconService>(MockBehavior.Strict);
        var helper = new IconTagHelper(service.Object) { Name = "cart" };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("icon", new TagHelperAttributeList(), (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        var condition = new IfTagHelper { Condition = false };
        condition.Init(context);
        await condition.ProcessAsync(context, output);
        await helper.ProcessAsync(context, output);
        service.VerifyNoOtherCalls();
        Assert.That(output.TagName, Is.Null);
    }

    /// <summary>
    /// Preserves caller presentation, encodes attributes and leaves cached payloads unchanged.
    /// </summary>
    [Test]
    public async Task Renders_Svg_Without_Mutating_Payload()
    {
        var svg = new IconSvg
        {
            ViewBox = "0 0 24 24",
            Content = "<path d=\"M0 0L1 1\" />",
            RootAttributes = new Dictionary<string, string>
            {
                ["fill"] = "none",
                ["style"] = "stroke-width:var(--icon-stroke-width,1.5);",
                ["class"] = "source-icon"
            }
        };
        var service = new Mock<IIconService>();
        using var cancellation = new CancellationTokenSource();
        var icon = new IconInfo { Name = "cart-01", Library = "hugeicons", Variant = "rounded" };
        service.Setup(x => x.GetIconAsync("cart", "hi", "rounded", cancellation.Token)).ReturnsAsync(icon);
        service.Setup(x => x.GetSvgAsync(icon, cancellation.Token)).ReturnsAsync(svg);
        var helper = new IconTagHelper(service.Object)
        {
            Name = "cart",
            Library = "hi",
            Variant = "rounded",
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token } }
        };
        var attributes = new TagHelperAttributeList
        {
            { "name", "cart" }, { "library", "hi" }, { "variant", "rounded" },
            { "class", "icon-2x" }, { "style", "--icon-stroke-width:2" },
            { "aria-label", "Cart \"<&" }
        };
        var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("icon", attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await helper.ProcessAsync(context, output);

        Assert.That(output.TagName, Is.EqualTo("svg"));
        Assert.That(output.TagMode, Is.EqualTo(TagMode.StartTagAndEndTag));
        Assert.That(output.Attributes.ContainsName("name"), Is.False);
        Assert.That(output.Attributes.ContainsName("library"), Is.False);
        Assert.That(output.Attributes.ContainsName("variant"), Is.False);
        Assert.That(output.Attributes["class"].Value.ToString(), Does.Contain("icon-2x").And.Contain("source-icon").And.Contain("icon"));
        Assert.That(output.Attributes["style"].Value.ToString(), Is.EqualTo("stroke-width:var(--icon-stroke-width,1.5); --icon-stroke-width:2"));
        Assert.That(output.Attributes.ContainsName("aria-hidden"), Is.False);
        Assert.That(output.Attributes["role"].Value, Is.EqualTo("img"));
        Assert.That(svg.RootAttributes["class"], Is.EqualTo("source-icon"));
        Assert.That(svg.RootAttributes["style"], Is.EqualTo("stroke-width:var(--icon-stroke-width,1.5);"));
        using var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        Assert.That(writer.ToString(), Does.Contain("&quot;&lt;&amp;").And.Contain("<path"));
        service.VerifyAll();
    }

    /// <summary>
    /// Suppresses unavailable icons and makes unlabeled icons decorative.
    /// </summary>
    /// <param name="found">Whether the service resolves an icon.</param>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Handles_Missing_And_Decorative_Icons(bool found)
    {
        var service = new Mock<IIconService>();
        var icon = new IconInfo { Name = "cart-01", Library = "hugeicons", Variant = "rounded" };
        service.Setup(x => x.GetIconAsync("cart", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(found ? icon : null);
        service.Setup(x => x.GetSvgAsync(icon, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IconSvg { ViewBox = "0 0 24 24", Content = "<path/>" });
        var helper = new IconTagHelper(service.Object)
        {
            Name = "cart",
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext() }
        };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("icon", new TagHelperAttributeList(), (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        await helper.ProcessAsync(context, output);
        if (found)
        {
            Assert.That(output.Attributes["aria-hidden"].Value, Is.EqualTo("true"));
            Assert.That(output.Attributes["focusable"].Value, Is.EqualTo("false"));
        }
        else
        {
            Assert.That(output.TagName, Is.Null);
            Assert.That(output.Content.GetContent(), Is.Empty);
        }
    }
}
