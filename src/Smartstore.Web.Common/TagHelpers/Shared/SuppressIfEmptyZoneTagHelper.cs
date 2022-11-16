using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Suppresses tag output if the specified child zone is all empty, that is: 
    /// has no widget that produces non-whitespace content. 
    /// Use this TagHelper on zone parent/container tags, 
    /// that should be removed if the inner zone is empty.
    /// </summary>
    [HtmlTargetElement("div", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("span", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("section", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("aside", Attributes = SuppressAttributeName)]
    [HtmlTargetElement("p", Attributes = SuppressAttributeName)]
    public class SuppressIfEmptyZoneTagHelper : SmartTagHelper
    {
        const string SuppressAttributeName = "sm-suppress-if-empty-zone";

        private readonly IWidgetSelector _widgetSelector;

        public SuppressIfEmptyZoneTagHelper(IWidgetSelector widgetSelector)
        {
            _widgetSelector = widgetSelector;
        }

        // Should run after IfTagHelper, but before SuppressIfEmptyTagHelper
        public override int Order => int.MinValue + 1000;

        protected override string GenerateTagId(TagHelperContext context)
            => null;

        /// <summary>
        /// Suppresses tag output if the specified zone is all empty, that is: 
        /// has no widget that produces non-whitespace content. 
        /// Use this TagHelper on zone parent/container tags, 
        /// that should be removed if the inner zone is empty.
        /// </summary>
        [HtmlAttributeName(SuppressAttributeName)]
        public string ZoneName { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (ZoneName.IsEmpty())
            {
                return;
            }

            var zoneContent = await _widgetSelector.GetContentAsync(ZoneName, ViewContext);
            if (zoneContent.IsEmptyOrWhiteSpace)
            {
                // Zone is empty: suppress output!
                output.SuppressOutput();
            }
            else
            {
                // Zone is NOT empty: push generated content to context items
                // so that the corresponding child zone can fetch and output
                // the pre-generated content, otherwise we would render
                // the zone twice, and we don't want that.
                context.Items["zone-content-" + ZoneName] = zoneContent;
            }
        }

        internal static ZoneHtmlContent GetZoneContent(TagHelperContext context, string zoneName)
        {
            if (context.Items.TryGetValue("zone-content-" + zoneName, out var content))
            {
                return content as ZoneHtmlContent;
            }

            return null;
        }
    }
}
