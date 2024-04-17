using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Platform.AI;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    // TODO: (mh) Rename --> AIImageTagHelper
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for image creation.
    /// </summary>
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class ImageCreationTagHelper : AITagHelperBase
    {
        // TODO: (mh) Rename --> ai-image
        const string EditorTagName = "ai-image-creation";

        const string FormatAttributeName = "format";
        const string MediaFolderAttributeName = "media-folder";

        private readonly AIToolHtmlGenerator _aiToolHtmlGenerator;

        public ImageCreationTagHelper(AIToolHtmlGenerator aiToolHtmlGenerator, IHtmlGenerator htmlGenerator)
            : base(htmlGenerator)
        {
            _aiToolHtmlGenerator = aiToolHtmlGenerator;
        }

        /// <summary>
        /// Used to be passed to AI provider to define the format of the picture about to be created.
        /// </summary>
        [HtmlAttributeName(FormatAttributeName)]
        public AIImageCreationFormat Format { get; set; }

        /// <summary>
        /// Used to define the MediaFolderName for the picture about to be created.
        /// </summary>
        [HtmlAttributeName(MediaFolderAttributeName)]
        public string MediaFolder { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            var attributes = GenerateDataAttributes();
            var tool = _aiToolHtmlGenerator.GenerateImageCreationTool(attributes);
            if (tool == null)
            {
                return;
            }

            output.WrapContentWith(tool);
        }

        private AttributeDictionary GenerateDataAttributes()
        {
            var attributes = new AttributeDictionary
            {
                // INFO: We can't just use For.Name here, because the target property might be a nested property.
                //["data-target-property"] = For.Name,
                ["data-target-property"] = GetHtmlId(),
                ["data-entity-name"] = EntityName,
                ["data-entity-type"] = EntityType,
                ["data-format"] = Format.ToString(),
                ["data-media-folder"] = MediaFolder
            };

            return attributes;
        }
    }
}