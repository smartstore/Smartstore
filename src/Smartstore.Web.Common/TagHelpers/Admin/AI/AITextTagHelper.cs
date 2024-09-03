using AngleSharp;
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
        [HtmlAttributeName(DisplayWordLimitAttributeName)]
        public bool DisplayWordLimit { get; set; } = true;  // TODO: (mh) (ai) commented-out in view. obsolete?

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
        public int WordCount { get; set; }    // TODO: (mh) (ai) this is not used anywhere (?)

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            // Check if target field has content & pass parameter accordingly.
            // INFO: Has content has to be checked to determine whether the optimization options should be enabled.
            var hasContent = await HasValueAsync(output);
            var attributes = GetTagHelperAttributes();

            if (EntityName.IsEmpty())
            {
                return;
            }

            var tool = AIToolHtmlGenerator.GenerateTextCreationTool(attributes, hasContent);
            if (tool == null)
            {
                return;
            }

            output.WrapContentWith(tool);
        }

        private static async Task<bool> HasValueAsync(TagHelperOutput output)
        {
            // TODO: (mh) (ai) Extremely bad API decision. This is a hack. We should not rely on the rendered HTML to determine whether a field has content. TBD with MC.
            var hasContent = false;
            var content = (await output.GetChildContentAsync()).GetContent();
            
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(content));

            var inputs = document.QuerySelectorAll("input:not([type='button']):not([type='submit']):not([type='reset']):not([type='checkbox']):not([type='radio'])");
            
            // Check whether any input or textarea has content
            foreach (var input in inputs)
            {
                if (input.GetAttribute("value").HasValue())
                {
                    hasContent = true;
                    break;
                }
            }

            if (!hasContent)
            {
                var textareas = document.QuerySelectorAll("textarea");
                foreach (var textarea in textareas)
                {
                    if (textarea.TextContent.HasValue())
                    {
                        hasContent = true;
                        break;
                    }
                }
            }

            return hasContent;
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