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
        var helper = new IconTagHelper(service.Object, new IconRenderer(service.Object, Mock.Of<IIconKitService>())) { Name = "cart" };
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
    /// <param name="librarySelector">The canonical library short name or system name fallback.</param>
    /// <param name="variantSelector">The canonical variant short name or name fallback.</param>
    [TestCase("hi", "sr")]
    [TestCase("hi", "rounded")]
    [TestCase("hugeicons", "sr")]
    [TestCase("hugeicons", "rounded")]
    public async Task Renders_Svg_Without_Mutating_Payload(string librarySelector, string variantSelector)
    {
        var svg = new IconSvg
        {
            Address = librarySelector + ":cart-01@" + variantSelector,
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
        var icon = new IconInfo
        {
            Name = "cart-01",
            LibraryName = "hugeicons",
            LibraryShortName = librarySelector == "hi" ? "hi" : null,
            VariantName = "rounded",
            VariantShortName = variantSelector == "sr" ? "sr" : null
        };
        service.Setup(x => x.GetIconAsync("cart", "hi", "rounded", cancellation.Token)).ReturnsAsync(icon);
        service.Setup(x => x.GetSvgAsync(icon, cancellation.Token)).ReturnsAsync(svg);
        var helper = new IconTagHelper(service.Object, new IconRenderer(service.Object, Mock.Of<IIconKitService>()))
        {
            Name = "cart",
            Library = "hi",
            Variant = "rounded",
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token } }
        };
        var attributes = new TagHelperAttributeList
        {
            { "name", "cart" }, { "lib", "hi" }, { "variant", "rounded" },
            { "class", "icon-2x" }, { "style", "--icon-stroke-width:2" },
            { "aria-label", "Cart \"<&" }
        };
        var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("icon", attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        await helper.ProcessAsync(context, output);

        Assert.That(output.TagName, Is.EqualTo("svg"));
        Assert.That(output.TagMode, Is.EqualTo(TagMode.StartTagAndEndTag));
        Assert.That(output.Attributes.ContainsName("name"), Is.False);
        Assert.That(output.Attributes.ContainsName("lib"), Is.False);
        Assert.That(output.Attributes.ContainsName("variant"), Is.False);
        Assert.That(output.Attributes["class"].Value.ToString(), Does.Contain("icon-2x").And.Contain("source-icon").And.Contain("icon"));
        Assert.That(output.Attributes["class"].Value.ToString().Split(' '), Does.Contain("icon-" + librarySelector));
        Assert.That(output.Attributes["class"].Value.ToString().Split(' '), Does.Contain("icon-" + librarySelector + "-" + variantSelector));
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
    /// Transfers explicit false and zero overrides and consumes all bound presentation attributes.
    /// </summary>
    [Test]
    public async Task Transfers_Presentation_Options_Exactly_Once()
    {
        var icon = new IconInfo { Name = "cart-01" };
        var service = new Mock<IIconService>();
        service.Setup(x => x.GetIconAsync("cart?flip=x", "hi", null, It.IsAny<CancellationToken>())).ReturnsAsync(icon);
        IconOptions captured = null;
        var renderer = new Mock<IIconRenderer>();
        renderer.Setup(x => x.RenderAsync(icon, It.IsAny<IconOptions>(), It.IsAny<CancellationToken>()))
            .Callback<IconInfo, IconOptions, CancellationToken>((_, options, _) => captured = options)
            .ReturnsAsync(new TagBuilder("svg"));
        var helper = new IconTagHelper(service.Object, renderer.Object)
        {
            Name = "cart?flip=x", Library = "hi", Rotate = 0, FlipHorizontal = false,
            StrokeScale = 1, Size = "3x", Animation = "beat-fade", CssClass = "custom", Style = "color:red",
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext() }
        };
        var attributes = new TagHelperAttributeList
        {
            { "name", helper.Name }, { "lib", "hi" }, { "rotate", 0 }, { "flip-h", false },
            { "stroke-scale", 1 }, { "size", "3x" }, { "animation", "beat-fade" },
            { "class", "custom" }, { "style", "color:red" }, { "data-test", "kept" }
        };
        var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("icon", attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        await helper.ProcessAsync(context, output);
        Assert.That(captured.Rotate, Is.EqualTo(0));
        Assert.That(captured.FlipHorizontal, Is.False);
        Assert.That(captured.StrokeScale, Is.EqualTo(1));
        Assert.That(captured.Animation, Is.EqualTo("beat-fade"));
        Assert.That(captured.Attributes, Has.Count.EqualTo(3));
        Assert.That(captured.Attributes["class"], Is.EqualTo("custom"));
        Assert.That(captured.Attributes["style"], Is.EqualTo("color:red"));
        Assert.That(captured.Attributes["data-test"], Is.EqualTo("kept"));
    }

    /// <summary>
    /// Uses an HTML host and retains independent SVG viewBoxes and layer order.
    /// </summary>
    [Test]
    public async Task Stack_Renders_Independent_Icon_Layers()
    {
        var helper = new IconStackTagHelper(new IconRenderer(Mock.Of<IIconService>(), Mock.Of<IIconKitService>()))
        {
            Size = "2x", Rotate = 90, FlipHorizontal = true, Animation = "spin",
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext() }
        };
        var children = new DefaultTagHelperContent().SetHtmlContent("<svg viewBox=\"0 0 16 16\"></svg><svg viewBox=\"0 0 24 24\"></svg>");
        var attributes = new TagHelperAttributeList { { "aria-label", "Confirmed" } };
        var context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("icon-stack", attributes, (_, _) => Task.FromResult(children));
        await helper.ProcessAsync(context, output);
        Assert.That(output.TagName, Is.EqualTo("span"));
        Assert.That(output.Attributes["class"].Value.ToString(), Does.Contain("icon-stack").And.Contain("icon-2x").And.Contain("icon-spin"));
        Assert.That(output.Attributes["style"].Value.ToString(), Does.Contain("--icon-rotate:90deg").And.Contain("--icon-flip-x:-1"));
        Assert.That(output.Attributes["role"].Value, Is.EqualTo("img"));
        Assert.That(output.Content.GetContent(), Is.EqualTo(children.GetContent()));
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
        var icon = new IconInfo { Name = "cart-01", LibraryName = "hugeicons", VariantName = "rounded" };
        service.Setup(x => x.GetIconAsync("cart", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(found ? icon : null);
        service.Setup(x => x.GetSvgAsync(icon, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IconSvg { Address = "hi:cart-01@rounded", ViewBox = "0 0 24 24", Content = "<path/>" });
        var helper = new IconTagHelper(service.Object, new IconRenderer(service.Object, Mock.Of<IIconKitService>()))
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
