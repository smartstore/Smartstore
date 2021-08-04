using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    [OutputElementHint("input")]
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class EditorTagHelper : BaseFormTagHelper
    {
        const string EditorTagName = "editor";
        const string TemplateAttributeName = "asp-template";
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

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            var htmlAttributes = new Dictionary<string, object>();
            var attrs = output.Attributes;
            
            if (attrs != null && attrs.Count > 0)
            {
                foreach (var attr in attrs)
                {
                    htmlAttributes[attr.Name] = attr.Value;
                }
            }

            output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, Template, new { htmlAttributes, postfix = Postfix }));
        }
    }
}
