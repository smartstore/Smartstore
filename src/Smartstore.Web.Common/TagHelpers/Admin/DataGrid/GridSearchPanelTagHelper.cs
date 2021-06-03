using System;
using System.Threading.Tasks;
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

        [HtmlAttributeNotBound]
        internal TagHelperContent Template { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            Template = new DefaultTagHelperContent();
            (await output.GetChildContentAsync()).CopyTo(Template);
            output.SuppressOutput();
        }
    }
}
