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
    public class SuppressIfEmptyZoneTagHelper : SmartTagHelper, IWidgetZone
    {
        const string SuppressAttributeName = "sm-suppress-if-empty-zone";
        const string PreviewDisabledAttributeName = "sm-preview-disabled";

        private readonly IWidgetSelector _widgetSelector;

        public SuppressIfEmptyZoneTagHelper(IWidgetSelector widgetSelector)
        {
            _widgetSelector = widgetSelector;
        }

        // Should run after IfTagHelper, but before SuppressIfEmptyTagHelper
        public override int Order => int.MinValue + 1000;

        /// <summary>
        /// Suppresses tag output if the specified zone is all empty, that is: 
        /// has no widget that produces non-whitespace content. 
        /// Use this TagHelper on zone parent/container tags, 
        /// that should be removed if the inner zone is empty.
        /// </summary>
        [HtmlAttributeName(SuppressAttributeName)]
        public string Name { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(PreviewDisabledAttributeName)]
        public bool PreviewDisabled { get; set; }

        /// <summary>
        /// Just to fuilfill the IWidgetZone contract.
        /// </summary>
        [HtmlAttributeNotBound]
        public bool ReplaceContent { get; }

        /// <summary>
        /// Just to fuilfill the IWidgetZone contract.
        /// </summary>
        [HtmlAttributeNotBound]
        public bool RemoveIfEmpty { get; }

        /// <summary>
        /// Just to fuilfill the IWidgetZone contract.
        /// </summary>
        [HtmlAttributeNotBound]
        public string PreviewTagName { get; }

        /// <summary>
        /// Just to fuilfill the IWidgetZone contract.
        /// </summary>
        [HtmlAttributeNotBound]
        public string PreviewCssClass { get; }

        /// <summary>
        /// Just to fuilfill the IWidgetZone contract.
        /// </summary>
        [HtmlAttributeNotBound]
        public string PreviewCssStyle { get; }

        protected override string GenerateTagId(TagHelperContext context)
            => null;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (Name.IsEmpty())
            {
                return;
            }

            if (!await _widgetSelector.HasContentAsync(this, ViewContext))
            {
                // Zone is empty: suppress output!
                output.SuppressOutput();
            }
        }
    }
}
