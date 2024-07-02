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

            if (await _widgetSelector.HasContentAsync(ZoneName, ViewContext))
            {
                // Zone is empty: suppress output!
                output.SuppressOutput();
            }
        }
    }
}
