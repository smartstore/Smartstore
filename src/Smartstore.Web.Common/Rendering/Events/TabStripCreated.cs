using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering.Builders;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Rendering.Events
{
    public class TabStripCreated
    {
        public TabStripCreated(TabStripTagHelper tabStrip, TagHelperContext context)
        {
            Guard.NotNull(tabStrip);

            TabStrip = tabStrip;
            TabFactory = new TabFactory(tabStrip, context);
            TabStripName = tabStrip.Id;
            Html = tabStrip.HtmlHelper;
            Model = tabStrip.ViewContext.ViewData.Model;
        }

        internal TabStripTagHelper TabStrip { get; }
        internal List<Widget> Widgets { get; private set; }

        public TabFactory TabFactory { get; }
        public string TabStripName { get; }
        public IHtmlHelper Html { get; }
        public object Model { get; }

        /// <summary>
        /// Gets the names of the tabs.
        /// </summary>
        public string[] TabNames
            => TabStrip.Tabs.Select(x => x.TabName).ToArray();

        /// <summary>
        /// Renders a widget into a dynamically created special tab called 'Plugins'.
        /// </summary>
        /// <param name="widget">Widget to render.</param>
        /// <remarks>Should only be called for admin tabstrips.</remarks>
        public void AddWidget(Widget widget)
        {
            Guard.NotNull(widget);

            (Widgets ??= []).Add(widget);
        }
    }
}
