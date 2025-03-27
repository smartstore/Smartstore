using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement(HtmlEditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class HtmlEditorTagHelper : BaseFormTagHelper
    {
        const string HtmlEditorTagName = "html-editor";
        const string TemplateAttributeName = "asp-template";
        const string SaveUrlAttributeName = "save-url";
        const string LazyAttributeName = "lazy";
        const string DefaultTemplateName = "Html";

        /// <summary>
        /// Specifies the editor template which will be used to render the field. Default = "Html".
        /// </summary>
        [HtmlAttributeName(TemplateAttributeName)]
        public string Template { get; set; } = DefaultTemplateName;

        /// <summary>
        /// Specifies the URL to save the editor content.
        /// </summary>
        [HtmlAttributeName(SaveUrlAttributeName)]
        public string SaveUrl { get; set; }

        /// <summary>
        /// Specifies whether the editor should be lazy-loaded. Default = true;
        /// </summary>
        [HtmlAttributeName(LazyAttributeName)]
        public bool Lazy { get; set; } = true;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            var template = Template ?? For.Metadata.TemplateHint ?? DefaultTemplateName;
            var data = new Dictionary<string, object>
            {
                ["saveUrl"] = SaveUrl,
                ["lazy"] = Lazy
            };

            output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, template, data));
        }
    }
}
