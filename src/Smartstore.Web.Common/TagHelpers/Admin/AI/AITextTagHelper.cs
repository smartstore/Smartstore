using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for text creation.
    /// </summary>
    [HtmlTargetElement("ai-text", Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class AITextTagHelper() : AITagHelperBase()
    {
        const string DisplayWordLimitAttributeName = "display-word-limit";
        const string DisplayStyleAttributeName = "display-style";
        const string DisplayToneAttributeName = "display-tone";
        const string DisplayOptimizationOptionsAttributeName = "display-optimization-options";
        const string WordCountAttributeName = "word-count";
        const string CharLimitAttributeName = "char-limit";

        /// <summary>
        /// Used to specify whether the word count should be displayed in the text creation dialog. Default = true.
        /// </summary>
        /// <remarks>
        /// This setting is needed e.g. for SEO texts like title and description, where the length of the answer is handled explicitly by the prompt. 
        /// </remarks>
        [HtmlAttributeName(DisplayWordLimitAttributeName)]
        public bool DisplayWordLimit { get; set; } = true;

        /// <summary>
        /// Used to specify whether the style option should be displayed in the text creation dialog. Default = true.
        /// </summary>
        [HtmlAttributeName(DisplayStyleAttributeName)]
        public bool DisplayStyle { get; set; } = true;

        /// <summary>
        /// Used to specify whether the tone option should be displayed in the text creation dialog. Default = true.
        /// </summary>
        [HtmlAttributeName(DisplayToneAttributeName)]
        public bool DisplayTone { get; set; } = true;

        /// <summary>
        /// Used to specify whether the optimization options should be displayed in the text creation dialog. Default = true.
        /// </summary>
        [HtmlAttributeName(DisplayOptimizationOptionsAttributeName)]
        public bool DisplayOptimizationOptions { get; set; } = true;

        /// <summary>
        /// Specifies the maximum number of characters that an AI response may have.
        /// Typically, this is the length of the associated database field.
        /// 0 (default) to not limit the length of the answer.
        /// </summary>
        [HtmlAttributeName(CharLimitAttributeName)]
        public int CharLimit { get; set; }

        /// <summary>
        /// Specifies the maximum number of words that an AI response may have.
        /// 0 (default) to not limit the length of the answer.
        /// </summary>
        [HtmlAttributeName(WordCountAttributeName)]
        public int WordCount { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            if (EntityName.IsEmpty())
            {
                return;
            }

            var enabled = For?.Model?.ToString()?.HasValue() ?? false;
            var attributes = GetTagHelperAttributes();

            var tool = AIToolHtmlGenerator.GenerateTextCreationTool(attributes, enabled);
            if (tool == null)
            {
                return;
            }

            output.WrapElementWith(InnerHtmlPosition.Append, tool);
        }

        protected override AttributeDictionary GetTagHelperAttributes()
        {
            var attrs = base.GetTagHelperAttributes();

            attrs["data-display-word-limit"] = DisplayWordLimit.ToString().ToLower();
            attrs["data-display-style"] = DisplayStyle.ToString().ToLower();
            attrs["data-display-tone"] = DisplayTone.ToString().ToLower();
            attrs["data-display-optimization-options"] = DisplayOptimizationOptions.ToString().ToLower();
            attrs["data-char-limit"] = CharLimit.ToStringInvariant();
            attrs["data-is-rich-text"] = "false";

            return attrs;
        }
    }
}