using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// TODO: (core) Describe
    /// </summary>
    [HtmlTargetElement("search-panel", ParentTag = "datagrid")]
    public class GridSearchPanelTagHelper : TagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.SearchPanel = this;
            }
        }

        /// <summary>
        /// Search panel width. Any CSS width specification is valid. Default: 350px;
        /// </summary>
        public string Width { get; set; } = "350px";

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();
            if (content.IsEmptyOrWhiteSpace)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "template";
            output.Attributes.Add("v-slot:search", "grid");
        }
    }
}
