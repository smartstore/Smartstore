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

        protected virtual string GetHtmlId()
        {
            var fullname = HtmlHelper.Name(For.Name);
            var id = HtmlHelper.GenerateIdFromName(fullname);

            return id;
        }
    }
}