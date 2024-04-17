using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    // TODO: (mh) Rename --> AIRichTextTagHelper
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for Html text creation.
    /// </summary>
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class RichTextCreationTagHelper : AITagHelperBase
    {
        // TODO: (mh) Rename --> ai-rich-text
        const string EditorTagName = "ai-rich-text-creation";

        const string DisplayAdditionalContentOptionsAttributeName = "display-additional-content-options";
        const string DisplayLinkOptionsAttributeName = "display-link-options";
        const string DisplayStructureOptionsAttributeName = "display-structure-options";
        const string DisplayImageOptionsAttributeName = "display-image-options";

        /// <summary>
        /// Defines whether the additional content options should be displayed in the text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayAdditionalContentOptionsAttributeName)]
        public bool DisplayAdditionalContentOptions { get; set; }

        /// <summary>
        /// Defines whether the link options should be displayed in the text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayLinkOptionsAttributeName)]
        public bool DisplayLinkOptions { get; set; }

        /// <summary>
        /// Defines whether the structure options should be displayed in the text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayStructureOptionsAttributeName)]
        public bool DisplayStructureOptions { get; set; } = true;

        /// <summary>
        /// Defines whether the image options should be displayed in the text creation dialog.
        /// </summary>
        [HtmlAttributeName(DisplayImageOptionsAttributeName)]
        public bool DisplayImageOptions { get; set; } = true;

        private readonly AIToolHtmlGenerator _aiToolHtmlGenerator;

        public RichTextCreationTagHelper(AIToolHtmlGenerator aiToolHtmlGenerator, IHtmlGenerator htmlGenerator)
            : base(htmlGenerator)
        {
            _aiToolHtmlGenerator = aiToolHtmlGenerator;
        }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            var attributes = GetTaghelperAttributes();
            var tool = _aiToolHtmlGenerator.GenerateRichTextTool(attributes);
            if (tool == null)
            {
                return;
            }

            output.WrapContentWith(tool);
        }

        private AttributeDictionary GetTaghelperAttributes()
        {
            var attributes = new AttributeDictionary
            {
                // INFO: We can't just use For.Name here, because the target property might be a nested property.
                //["data-target-property"] = For.Name,
                ["data-target-property"] = GetHtmlId(),
                ["data-entity-name"] = EntityName,
                ["data-entity-type"] = EntityType,
                ["data-entity-id"] = EntityId,
                ["data-is-rich-text"] = "true",
                ["data-display-additional-content-options"] = DisplayAdditionalContentOptions.ToString().ToLower(),
                ["data-display-link-options"] = DisplayLinkOptions.ToString().ToLower(),
                ["data-display-image-options"] = DisplayImageOptions.ToString().ToLower(),
                ["data-display-structure-options"] = DisplayStructureOptions.ToString().ToLower()
            };

            return attributes;
        }
    }
}