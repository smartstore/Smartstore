using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("modal-footer", ParentTag = "modal")]
    public class ModalFooterTagHelper : SmartTagHelper
    {
        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AppendCssClass("modal-footer");

            await output.LoadAndSetChildContentAsync();
        }

        protected override string GenerateTagId(TagHelperContext context) 
            => null;
    }
}
