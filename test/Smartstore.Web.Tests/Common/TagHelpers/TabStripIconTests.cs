using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Content.Media.Icons;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Tests.Common.TagHelpers;

[TestFixture]
public class TabStripIconTests
{
    [TestCase("cart", true)]
    [TestCase("bi:telephone!", true)]
    [TestCase("hi:missing!", false)]
    public async Task Renders_svg_preserves_classes_and_skips_empty_icons(string address, bool found)
    {
        using var cancellation = new CancellationTokenSource();
        var icon = new IconInfo { Name = "source" };
        var service = new Mock<IIconService>(MockBehavior.Strict);
        var renderer = new Mock<IIconRenderer>(MockBehavior.Strict);
        service.Setup(x => x.GetIconAsync(address, null, null, cancellation.Token)).ReturnsAsync(found ? icon : null);
        IconOptions received = null;
        if (found)
        {
            renderer.Setup(x => x.RenderAsync(icon, It.IsAny<IconOptions>(), cancellation.Token))
                .Callback((IconInfo _, IconOptions options, CancellationToken _) => received = options)
                .ReturnsAsync(new TagBuilder("svg"));
        }

        var viewContext = new ViewContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellation.Token } };
        var html = new Mock<IHtmlHelper>();
        html.SetupGet(x => x.ViewContext).Returns(viewContext);
        var builder = new ContainerBuilder();
        builder.RegisterInstance(service.Object).As<IIconService>();
        builder.RegisterInstance(renderer.Object).As<IIconRenderer>();
        builder.RegisterInstance(html.Object).As<IHtmlHelper>();
        using var container = builder.Build();
        using var services = new AutofacServiceProvider(container);
        viewContext.HttpContext.RequestServices = services;

        var items = new Dictionary<object, object>();
        var context = new TagHelperContext("tabstrip", new TagHelperAttributeList(), items, "strip");
        var strip = new TabStripTagHelper
        {
            Id = "strip",
            ViewContext = viewContext,
            PublishEvent = false,
            SmartTabSelection = TabSelectionHandling.None,
            Position = TabsPosition.Left
        };
        strip.Init(context);
        foreach (var name in new[] { address, null })
        {
            var tab = new TabTagHelper { ViewContext = viewContext, Icon = name, IconClass = "icon-lg text-warning" };
            var childContext = new TagHelperContext("tab", new TagHelperAttributeList(), items, "child");
            tab.Init(childContext);
            await tab.ProcessAsync(childContext, Output("tab", "Body"));
        }

        var output = Output("tabstrip", string.Empty);
        await strip.ProcessAsync(context, output);
        var content = output.Content.GetContent();
        Assert.That(content.Contains("<svg"), Is.EqualTo(found));
        Assert.That(content, Does.Contain("icon icon-fw"));
        Assert.That(content, Does.Not.Contain("fa-fw"));
        if (found)
        {
            Assert.That(received.Attributes["class"], Is.EqualTo("nav-icon icon-fw icon-lg text-warning"));
        }
        service.VerifyAll();
        renderer.VerifyAll();
    }

    private static TagHelperOutput Output(string tag, string content)
        => new(tag, new TagHelperAttributeList(), (_, _) =>
            Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(content)));
}
