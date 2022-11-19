using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering.Builders;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Rendering.Events
{
    /// <summary>
    /// Tabstrip created event
    /// </summary>
    public class TabStripCreated
    {
        public TabStripCreated(TabStripTagHelper tabStrip, TagHelperContext context)
        {
            Guard.NotNull(tabStrip, nameof(tabStrip));

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
        /// Renders a widget into a dynamically created special tab called 'Plugins' 
        /// </summary>
        /// <param name="widget">Widget to render</param>
        /// <remarks>Should only be called for admin tabstrips</remarks>
        public void AddWidget(Widget widget)
        {
            Guard.NotNull(widget, nameof(widget));

            (Widgets ??= new List<Widget>()).Add(widget);
        }
    }
}
