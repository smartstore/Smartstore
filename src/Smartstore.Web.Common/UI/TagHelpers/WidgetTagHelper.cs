using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    [HtmlTargetElement(WidgetTagName, Attributes = TargetAttributeName)]
    public class WidgetTagHelper : SmartTagHelper
    {
        const string UniqueKeysKey = "WidgetTagHelper.UniqueKeys";
        const string WidgetTagName = "widget";
        const string TargetAttributeName = "target";
        const string OrderAttributeName = "order";

        private readonly IWidgetProvider _widgetProvider;

        public WidgetTagHelper(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        /// <summary>
        /// The target zone name to inject this widget to.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// The order within the target zone.
        /// </summary>
        [HtmlAttributeName(OrderAttributeName)]
        public int Ordinal { get; set; }

        /// <summary>
        /// Whether the widget output should be inserted BEFORE target zone's existing content. 
        /// Omitting this attribute renders widget output AFTER any existing content.
        /// </summary>
        public bool Prepend { get; set; }

        /// <summary>
        /// When set, ensures uniqueness within a request
        /// </summary>
        public string Key { get; set; }

        protected override string GenerateTagId(TagHelperContext context) => null;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            if (Target.IsEmpty())
            {
                return;
            }

            if (Key.HasValue())
            {
                var uniqueKeys = GetUniqueKeys();
                if (uniqueKeys.Contains(Key))
                {
                    return;
                }
                uniqueKeys.Add(Key);
            }
            
            var childContent = await output.GetChildContentAsync();
            var widget = new HtmlWidgetInvoker(childContent) { Order = Ordinal, Prepend = Prepend };

            _widgetProvider.RegisterWidget(Target, widget);
        }

        private HashSet<string> GetUniqueKeys()
        {
            return ViewContext.HttpContext.GetItem(UniqueKeysKey, () => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
