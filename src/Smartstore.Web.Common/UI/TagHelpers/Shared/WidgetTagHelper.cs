using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers.Shared
{
    [HtmlTargetElement(WidgetTagName, Attributes = TargetZoneAttributeName)]
    public class WidgetTagHelper : SmartTagHelper
    {
        const string WidgetTagName = "widget";
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
            output.SuppressOutput();

            if (TargetZone.IsEmpty())
            {
                return;
            }

            if (Key.HasValue() && _widgetProvider.ContainsWidget(TargetZone, Key))
            {
                return;
            }
            
            var childContent = await output.GetChildContentAsync();
            var widget = new HtmlWidgetInvoker(childContent) { Order = Ordinal, Prepend = Prepend, Key = Key };

            _widgetProvider.RegisterWidget(TargetZone, widget);
        }
    }
}
