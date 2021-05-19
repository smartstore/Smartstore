using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    /// <summary>
    /// Template for the toolbar content as Vue slot template. Root object is called <c>grid</c>
    /// and provides the following members: <c>selectedRows, selectedRowsCount, selectedRowKeys, hasSelection, command, rows, edit, submitChanges(), cancelEdit(), deleteSelected()</c>
    /// </summary>
    [HtmlTargetElement("toolbar", ParentTag = "datagrid")]
    public class GridToolbarTagHelper : TagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Toolbar = this;
            }
        }

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
