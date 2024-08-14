using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for HTML rich text creation.
    /// </summary>
    [HtmlTargetElement("ai-rich-text", Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class AIRichTextTagHelper() : AITagHelperBase()
    {
        // TODO: (mh) (ai) Bad naming. More specific please.
        const string DisplayAdditionalContentOptionsAttributeName = "display-additional-content-options";
        const string DisplayLinkOptionsAttributeName = "display-link-options";
        const string DisplayStructureOptionsAttributeName = "display-structure-options";
        const string DisplayImageOptionsAttributeName = "display-image-options";

        /// <summary>
        /// Defines whether the additional content options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayAdditionalContentOptionsAttributeName)]
        public bool DisplayAdditionalContentOptions { get; set; }

        /// <summary>
        /// Defines whether the link options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayLinkOptionsAttributeName)]
        public bool DisplayLinkOptions { get; set; }

        /// <summary>
        /// Defines whether the structure options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayStructureOptionsAttributeName)]
        public bool DisplayStructureOptions { get; set; } = true;

        /// <summary>
        /// Defines whether the image options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayImageOptionsAttributeName)]
        public bool DisplayImageOptions { get; set; } = true;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            var attributes = GetTagHelperAttributes();
            var tool = AIToolHtmlGenerator.GenerateRichTextTool(attributes);
            if (tool == null)
            {
                return;
            }

            output.WrapContentWith(tool);
        }

        protected override AttributeDictionary GetTagHelperAttributes()
        {
            var attrs = base.GetTagHelperAttributes();

            attrs["data-display-additional-content-options"] = DisplayAdditionalContentOptions.ToString().ToLower();
            attrs["data-display-link-options"] = DisplayLinkOptions.ToString().ToLower();
            attrs["data-display-image-options"] = DisplayImageOptions.ToString().ToLower();
            attrs["data-display-structure-options"] = DisplayStructureOptions.ToString().ToLower();
            attrs["data-is-rich-text"] = "true";

            return attrs;
        }
    }
}