using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    // TODO: (mh) Rename --> AISuggestionTagHelper
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for text suggestions.
    /// </summary>
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class SuggestionTagHelper : AITagHelperBase
    {
        // TODO: (mh) Rename --> ai-suggestion
        const string EditorTagName = "ai-suggestion";

        const string MandatoryEntityFieldsAttributeName = "mandatory-entity-fields";

        /// <summary>
        /// List of comma separated mandatory fields of the target entity.
        /// Used to fill them with a placeholder after the suggestion has been accepted.
        /// Thus the entity can be saved directly after a suggestion has been accepted.
        /// </summary>
        [HtmlAttributeName(MandatoryEntityFieldsAttributeName)]
        public string MandatoryEntityFields { get; set; }

        private readonly AIToolHtmlGenerator _aiToolHtmlGenerator;

        public SuggestionTagHelper(IHtmlGenerator htmlGenerator, AIToolHtmlGenerator aiToolHtmlGenerator)
            : base(htmlGenerator)
        {
            _aiToolHtmlGenerator = aiToolHtmlGenerator;
        }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            var attributes = GetTaghelperAttributes();
            var tool = _aiToolHtmlGenerator.GenerateSuggestionTool(attributes);
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
                ["data-mandatory-entity-fields"] = MandatoryEntityFields
            };

            return attributes;
        }
    }
}