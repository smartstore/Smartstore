using AngleSharp;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    // TODO: (mh) Rename --> AITextTagHelper
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for text creation.
    /// </summary>
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class TextCreationTagHelper : AITagHelperBase
    {
        // TODO: (mh) Rename --> ai-text
        const string EditorTagName = "ai-text-creation";

        const string DisplayWordLimitAttributeName = "display-word-limit";
        const string DisplayStyleAttributeName = "display-style";
        const string DisplayToneAttributeName = "display-tone";
        const string DisplayOptimizationOptionsAttributeName = "display-optimization-options";
        const string WordCountAttributeName = "word-count";

        private readonly AIToolHtmlGenerator _aiToolHtmlGenerator;

        public TextCreationTagHelper(IHtmlGenerator htmlGenerator, AIToolHtmlGenerator aiToolHtmlGenerator)
            : base(htmlGenerator)
        {
            _aiToolHtmlGenerator = aiToolHtmlGenerator;
        }

        /// <summary>
        /// Used to specify whether the word count should be displayed in the text creation dialog. Default = true.
        /// </summary>
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
        /// Used to specify the maximum word count for the text about to be created. Default = 50.
        /// </summary>
        [HtmlAttributeName(WordCountAttributeName)]
        public int WordCount { get; set; } = 50;

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
            var tool = _aiToolHtmlGenerator.GenerateTextCreationTool(attributes, hasContent, EntityName);
            if (tool == null)
            {
                return;
            }

            output.WrapContentWith(tool);
        }

        private static async Task<bool> HasValueAsync(TagHelperOutput output)
        {
            var hasContent = false;
            var content = (await output.GetChildContentAsync()).GetContent();
            
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(content));

            var inputs = document.QuerySelectorAll("input:not([type='button']):not([type='submit']):not([type='reset']):not([type='checkbox']):not([type='radio'])");
            var textareas = document.QuerySelectorAll("textarea");

            // Check whether any input or textarea has content
            foreach (var input in inputs)
            {
                if (input.GetAttribute("value").HasValue())
                {
                    hasContent = true;
                }
            }

            foreach (var textarea in textareas)
            {
                if (textarea.TextContent.HasValue())
                {
                    hasContent = true;
                }
            }

            return hasContent;
        }

        private AttributeDictionary GetTagHelperAttributes()
        {
            var attributes = new AttributeDictionary
            {
                // INFO: We can't just use For.Name here, because the target property might be a nested property.
                //["data-target-property"] = For.Name,
                ["data-target-property"] = GetHtmlId(),
                ["data-entity-name"] = EntityName.UrlEncode(),

                ["data-entity-type"] = EntityType,
                ["data-display-word-limit"] = DisplayWordLimit.ToString().ToLower(),
                ["data-display-style"] = DisplayStyle.ToString().ToLower(),
                ["data-display-tone"] = DisplayTone.ToString().ToLower(),
                ["data-display-optimization-options"] = DisplayOptimizationOptions.ToString().ToLower(),
                ["data-is-rich-text"] = "false"
            };

            return attributes;
        }
    }
}