using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Utilities;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    [OutputElementHint("input")]
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class EditorTagHelper : BaseFormTagHelper
    {
        const string EditorTagName = "editor";
        const string TemplateAttributeName = "asp-template";
        const string AdditionalViewDataAttributeName = "asp-additional-viewdata";
        //const string ValueAttributeName = "asp-value";
        const string PostfixAttributeName = "sm-postfix";

        // TODO: (mh) (core) Find a way to propagate "required" metadata to template, 'cause FluentValidation obviously does not provide this info to MVC metadata.

        /// <summary>
        /// Specifies the editor template which will be used to render the field.
        /// </summary>
        [HtmlAttributeName(TemplateAttributeName)]
        public string Template { get; set; }

        /// <summary>
        /// Specifies the value to set into editor input tag.
        /// </summary>
        //[HtmlAttributeName(ValueAttributeName)]
        //public string Value { get; set; }

        /// <summary>
        /// The text which will be displayed inside the input as a post fix.
        /// </summary>
        [HtmlAttributeName(PostfixAttributeName)]
        public string Postfix { get; set; }

        /// <summary>
        /// An anonymous <see cref="object"/> or <see cref="System.Collections.Generic.IDictionary{String, Object}"/>
        /// that can contain additional view data that will be merged into the
        /// <see cref="ViewDataDictionary{TModel}"/> instance created for the template.
        /// </summary>
        [HtmlAttributeName(AdditionalViewDataAttributeName)]
        public object AdditionalViewData { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            var viewData = ConvertUtility.ObjectToDictionary(AdditionalViewData);

            if (Postfix != null)
            {
                viewData["postfix"] = Postfix;
            }

            if (output.Attributes != null && output.Attributes.Count > 0)
            {
                var htmlAttributes = new Dictionary<string, object>();

                foreach (var attr in output.Attributes)
                {
                    htmlAttributes[attr.Name] = attr.ValueAsString();
                }

                viewData["htmlAttributes"] = htmlAttributes;
            }

            if (viewData.Count > 0)
            {
                output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, Template, viewData));
            }
            else
            {
                output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, Template));
            }
        }
    }
}
