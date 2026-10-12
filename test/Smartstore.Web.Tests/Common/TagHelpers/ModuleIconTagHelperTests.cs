using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using Smartstore.IconKit;
using Smartstore.Engine.Modularity;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Tests.Common.TagHelpers;

[TestFixture]
public class ModuleIconTagHelperTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Renders_Resolved_Representation(bool useAddress)
    {
        var viewContext = new ViewContext { HttpContext = new DefaultHttpContext() };
        var html = new Mock<IHtmlHelper>();
        html.SetupGet(x => x.ViewContext).Returns(viewContext);
        var service = new Mock<IIconService>(MockBehavior.Strict);
        var renderer = new Mock<IIconRenderer>(MockBehavior.Strict);
        var icon = new IconInfo { Name = "cart" };
        if (useAddress)
        {
            service.Setup(x => x.GetIconAsync("cart?color=#123456", null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(icon);
            renderer.Setup(x => x.RenderAsync(icon, It.IsAny<IconOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TagBuilder("svg"));
        }
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.Content("~/Modules/Test/icon.svg")).Returns("/Modules/Test/icon.svg");
        var factory = new Mock<IUrlHelperFactory>();
        factory.Setup(x => x.GetUrlHelper(viewContext)).Returns(url.Object);
        var builder = new ContainerBuilder();
        builder.RegisterInstance(html.Object);
        builder.RegisterInstance(service.Object);
        builder.RegisterInstance(renderer.Object);
        builder.RegisterInstance(factory.Object);
        using var container = builder.Build();
        viewContext.HttpContext.RequestServices = new AutofacServiceProvider(container);
        var helper = new ModuleIconTagHelper
        {
            ViewContext = viewContext,
            Model = useAddress ? new ModuleIcon { Address = "cart?color=#123456" }
                : new ModuleIcon { ImageUrl = "~/Modules/Test/icon.svg" },
            Alt = "Test module"
        };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = CreateOutput();
        output.Attributes.SetAttribute("class", "custom");

        await helper.ProcessAsync(context, output);

        Assert.That(output.TagName, Is.EqualTo("span"));
        Assert.That(output.Attributes["class"].Value.ToString(), Does.Contain("custom").And.Contain("module-icon-display"));
        Assert.That(output.Attributes["aria-label"].Value, Is.EqualTo("Test module"));
        Assert.That(output.Content.GetContent(), Does.Contain(useAddress ? "<svg" : "src=\"/Modules/Test/icon.svg\""));
        service.VerifyAll();
        renderer.VerifyAll();
    }

    [Test]
    public async Task Suppressed_Module_Icon_Does_Not_Resolve_Services()
    {
        var helper = new ModuleIconTagHelper { Model = new ModuleIcon { Address = "cart" } };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = CreateOutput();
        var condition = new IfTagHelper { Condition = false };
        condition.Init(context);
        await condition.ProcessAsync(context, output);

        await helper.ProcessAsync(context, output);

        Assert.That(output.TagName, Is.Null);
    }

    private static TagHelperOutput CreateOutput()
        => new("module-icon", new TagHelperAttributeList(), (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
}
