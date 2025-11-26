using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.AI;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for image creation.
    /// </summary>
    [HtmlTargetElement("ai-image", Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class AIImageTagHelper() : AITagHelperBase()
    {
        const string OrientationAttributeName = "format";
        const string MediaFolderAttributeName = "media-folder";

        /// <summary>
        /// Passed to AI provider to define the orientation of the picture about to be created.
        /// </summary>
        [HtmlAttributeName(OrientationAttributeName)]
        public AIImageOrientation Orientation { get; set; }

        /// <summary>
        /// Used to define the MediaFolderName for the picture about to be created.
        /// </summary>
        [HtmlAttributeName(MediaFolderAttributeName)]
        public string MediaFolder { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            if (EntityName.IsEmpty())
            {
                return;
            }

            var attributes = GetTagHelperAttributes();
            var tool = AIToolHtmlGenerator.GenerateImageCreationTool(attributes);
            if (tool == null)
            {
                return;
            }

            output.WrapElementWith(InnerHtmlPosition.Append, tool);
        }

        protected override AttributeDictionary GetTagHelperAttributes()
        {
            var attrs = base.GetTagHelperAttributes();

            attrs["data-orientation"] = Orientation.ToString();
            attrs["data-media-folder"] = MediaFolder;

            return attrs;
        }
    }
}