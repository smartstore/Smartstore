using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("detail-view", ParentTag = "datagrid")]
    public class GridDetailViewTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();
            if (content.IsEmptyOrWhiteSpace)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "template";
            output.Attributes.Add("v-slot:detailview", "item");
        }
    }
}
