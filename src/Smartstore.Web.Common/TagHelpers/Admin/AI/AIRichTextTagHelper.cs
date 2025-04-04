using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Utilities.Html;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for HTML rich text creation.
    /// </summary>
    [HtmlTargetElement("ai-rich-text", Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class AIRichTextTagHelper() : AITagHelperBase()
    {
        const string DisplayLanguageOptionsAttributeName = "display-language-options";
        const string DisplayTocOptionsAttributeName = "display-toc-options";
        const string DisplayLinkOptionsAttributeName = "display-link-options";
        const string DisplayLayoutOptionsAttributeName = "display-layout-options";
        const string DisplayImageOptionsAttributeName = "display-image-options";

        /// <summary>
        /// Defines whether the options to set language, tone & style should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayLanguageOptionsAttributeName)]
        public bool DisplayLanguageOptions { get; set; } = true;

        /// <summary>
        /// Defines whether the options to create a table of contents should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayTocOptionsAttributeName)]
        public bool DisplayTocOptions { get; set; }

        /// <summary>
        /// Defines whether the link options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayLinkOptionsAttributeName)]
        public bool DisplayLinkOptions { get; set; }

        /// <summary>
        /// Defines whether the structure options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayLayoutOptionsAttributeName)]
        public bool DisplayLayoutOptions { get; set; } = true;

        /// <summary>
        /// Defines whether the image options should be displayed in the rich text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayImageOptionsAttributeName)]
        public bool DisplayImageOptions { get; set; } = true;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            if (EntityName.IsEmpty())
            {
                return;
            }

            var enabled = !HtmlUtility.IsEmptyHtml(For?.Model?.ToString());
            var attributes = GetTagHelperAttributes();
            var tool = AIToolHtmlGenerator.GenerateRichTextTool(attributes, enabled);
            if (tool == null)
            {
                return;
            }

            output.WrapElementWith(InnerHtmlPosition.Append, tool);
        }

        protected override AttributeDictionary GetTagHelperAttributes()
        {
            var attrs = base.GetTagHelperAttributes();

            attrs["data-display-language-options"] = DisplayLanguageOptions.ToString().ToLower();
            attrs["data-display-toc-options"] = DisplayTocOptions.ToString().ToLower();
            attrs["data-display-link-options"] = DisplayLinkOptions.ToString().ToLower();
            attrs["data-display-image-options"] = DisplayImageOptions.ToString().ToLower();
            attrs["data-display-layout-options"] = DisplayLayoutOptions.ToString().ToLower();
            attrs["data-is-rich-text"] = "true";

            return attrs;
        }
    }
}