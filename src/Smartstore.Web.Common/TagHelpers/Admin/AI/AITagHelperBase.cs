using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// The base implementation for AI tag helpers.
    /// </summary>
    public class AITagHelperBase : BaseFormTagHelper
    {
        const string EntityIdAttributeName = "entity-id";
        const string EntityNameAttributeName = "entity-name";
        const string EntityTypeAttributeName = "entity-type";

        /// <summary>
        /// Used to determine which prompt should be used to create the text.
        /// </summary>
        [HtmlAttributeName(EntityTypeAttributeName)]
        public string EntityType { get; set; }

        /// <summary>
        /// Used to obtain an entity from the database when needed.
        /// </summary>
        [HtmlAttributeName(EntityIdAttributeName)]
        public string EntityId { get; set; }

        /// <summary>
        /// Used to be passed to AI provider to describe the text about to be created.
        /// </summary>
        [HtmlAttributeName(EntityNameAttributeName)]
        public string EntityName { get; set; }

        private readonly IHtmlGenerator _htmlGenerator;

        public AITagHelperBase(IHtmlGenerator htmlGenerator)
        {
            _htmlGenerator = htmlGenerator;
        }

        protected virtual string GetHtmlId()
        {
            // TODO: (mh) Use HtmlHelper.IdFor<>(). TBD with MC.
            var tagBuilder = _htmlGenerator.GenerateLabel(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                labelText: null,
                htmlAttributes: null);

            return tagBuilder.Attributes["for"];
        }
    }
}