using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Renders a button or dropdown (depending on the number of active AI providers) to open a dialog for text suggestions.
    /// </summary>
    [HtmlTargetElement("ai-suggestion", Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class AISuggestionTagHelper() : AITagHelperBase()
    {
        const string MandatoryEntityFieldsAttributeName = "mandatory-entity-fields";
        const string CharLimitAttributeName = "char-limit";

        /// <summary>
        /// List of comma separated mandatory fields of the target entity.
        /// Used to fill them with a placeholder after the suggestion has been accepted.
        /// Thus the entity can be saved directly after a suggestion has been accepted.
        /// </summary>
        [HtmlAttributeName(MandatoryEntityFieldsAttributeName)]
        public string MandatoryEntityFields { get; set; }

        /// <summary>
        /// Specifies the maximum number of characters that an AI response may have.
        /// Typically, this is the length of the associated database field.
        /// 0 (default) to not limit the length of the answer.
        /// </summary>
        [HtmlAttributeName(CharLimitAttributeName)]
        public int CharLimit { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;

            var attributes = GetTagHelperAttributes();
            var tool = AIToolHtmlGenerator.GenerateSuggestionTool(attributes);
            if (tool == null)
            {
                return;
            }

            output.WrapElementWith(InnerHtmlPosition.Append, tool);
        }

        protected override AttributeDictionary GetTagHelperAttributes()
        {
            var attrs = base.GetTagHelperAttributes();
            attrs["data-mandatory-entity-fields"] = MandatoryEntityFields;
            attrs["data-char-limit"] = CharLimit.ToStringInvariant();

            return attrs;
        }
    }
}