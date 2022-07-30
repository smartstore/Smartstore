using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("modal-body", ParentTag = "modal")]
    public class ModalBodyTagHelper : SmartTagHelper
    {
        const string ContentUrlAttributeName = "sm-content-url";

        /// <summary>
        /// The URL of the body's remote content. If set, an IFrame will replace the child content.
        /// </summary>
        [HtmlAttributeName(ContentUrlAttributeName)]
        public string ContentUrl { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AppendCssClass("modal-body");

            var content = await output.GetChildContentAsync();

            if (!content.IsEmptyOrWhiteSpace)
            {
                output.Content.SetHtmlContent(content);
            }
            else if (ContentUrl.HasValue())
            {
                output.Content.SetHtmlContent("<iframe class='modal-flex-fill-area' frameborder='0' src='{0}' />".FormatInvariant(UrlHelper.Content(ContentUrl)));
            }
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
