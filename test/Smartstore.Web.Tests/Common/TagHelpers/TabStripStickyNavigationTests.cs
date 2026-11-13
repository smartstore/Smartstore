using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Tests.Common.TagHelpers;

[TestFixture]
public class TabStripStickyNavigationTests
{
    [TestCase(TabsPosition.Left, true, true)]
    [TestCase(TabsPosition.Right, true, true)]
    [TestCase(TabsPosition.Top, true, false)]
    [TestCase(TabsPosition.Below, true, false)]
    [TestCase(TabsPosition.Left, false, false)]
    public async Task Sticky_navigation_is_opt_in_and_only_rendered_for_stacked_tabs(TabsPosition position, bool enabled, bool expected)
    {
        var viewContext = new ViewContext { HttpContext = new DefaultHttpContext() };
        var items = new Dictionary<object, object>();
        var context = new TagHelperContext("tabstrip", new TagHelperAttributeList(), items, "strip");
        var strip = new TabStripTagHelper
        {
            Id = "strip",
            ViewContext = viewContext,
            PublishEvent = false,
            SmartTabSelection = TabSelectionHandling.None,
            Position = position
        };
        Assert.That(strip.NavSticky, Is.False);
        strip.NavSticky = enabled;
        strip.Init(context);

        var tab = new TabTagHelper { ViewContext = viewContext, Title = "Example" };
        var childContext = new TagHelperContext("tab", new TagHelperAttributeList(), items, "child");
        tab.Init(childContext);
        await tab.ProcessAsync(childContext, Output("tab", "Body"));

        var output = Output("tabstrip", string.Empty);
        await strip.ProcessAsync(context, output);

        var content = output.Content.GetContent();
        Assert.That(content.Contains("nav-sticky"), Is.EqualTo(expected));
        Assert.That(content, Does.Not.Contain("custom-scrollbar"));
        Assert.That(content.Contains("scroll-fade-touch"), Is.EqualTo(position is TabsPosition.Left or TabsPosition.Right));
        Assert.That(content.Contains("scroll-fade-observable"), Is.EqualTo(position is TabsPosition.Left or TabsPosition.Right));
        Assert.That(content, Does.Not.Contain("scroll-fade-bleed"));
    }

    private static TagHelperOutput Output(string tag, string content)
        => new(tag, new TagHelperAttributeList(), (_, _) =>
            Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(content)));
}
