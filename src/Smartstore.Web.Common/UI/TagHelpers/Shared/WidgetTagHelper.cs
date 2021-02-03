using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers.Shared
{
    [HtmlTargetElement("widget", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("script", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("link", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("div", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("span", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("section", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("ul", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("ol", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("svg", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("img", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("a", Attributes = TargetZoneAttributeName)]
    public class WidgetTagHelper : SmartTagHelper
    {
        const string TargetZoneAttributeName = "zone";
        const string OrderAttributeName = "order";
        const string PrependAttributeName = "prepend";
        const string KeyAttributeName = "key";

        private readonly IWidgetProvider _widgetProvider;

        public WidgetTagHelper(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        /// <summary>
        /// The target zone name to inject this widget to.
        /// </summary>
        [HtmlAttributeName(TargetZoneAttributeName)]
        public string TargetZone { get; set; }

        /// <summary>
        /// The order within the target zone.
        /// </summary>
        [HtmlAttributeName(OrderAttributeName)]
        public int Ordinal { get; set; }

        /// <summary>
        /// Whether the widget output should be inserted BEFORE target zone's existing content. 
        /// Omitting this attribute renders widget output AFTER any existing content.
        /// </summary>
        [HtmlAttributeName(PrependAttributeName)]
        public bool Prepend { get; set; }

        /// <summary>
        /// When set, ensures uniqueness within a particular zone.
        /// </summary>
        [HtmlAttributeName(KeyAttributeName)]
        public string Key { get; set; }

        protected override string GenerateTagId(TagHelperContext context) 
            => null;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (TargetZone.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }

            if (Key.HasValue() && _widgetProvider.ContainsWidget(TargetZone, Key))
            {
                output.SuppressOutput();
                return;
            }

            var childContent = await output.GetChildContentAsync();

            TagHelperContent content;

            if (output.TagName == "widget")
            {
                // Never render <widget> tag, only the content
                output.TagName = null;
                content = childContent;
            }
            else
            {
                childContent.CopyTo(output.Content);
                //output.Content.SetHtmlContent(childContent);
                content = output.ToTagHelperContent();
            }

            output.SuppressOutput();

            var widget = new HtmlWidgetInvoker(content) { Order = Ordinal, Prepend = Prepend, Key = Key };
            _widgetProvider.RegisterWidget(TargetZone, widget);
        }
    }
}
