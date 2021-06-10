using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    public enum DataGridRowAction
    {
        Edit,
        Delete
    }

    /// <summary>
    /// Row commands (the last sticky column) dropdown markup. Passed object provides following members:
    /// <code>
    /// {
    ///     options,
    ///     dataSource,
    ///     columns,
    ///     paging,
    ///     sorting,
    ///     filtering,
    ///     item: {
    ///         row,
    ///         activateEdit(),
    ///         deleteRows()
    ///     }
    /// }
    /// </code>
    /// </summary>
    [HtmlTargetElement("row-commands", ParentTag = "datagrid")]
    [RestrictChildren("a", "div")]
    public class GridRowCommandsTagHelper : TagHelper
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
            output.Attributes.Add("v-slot:rowcommands", "item");

            var div = new TagBuilder("div");
            div.Attributes.Add("class", "dg-commands-dropdown dropdown-menu dropdown-menu-right");

            output.WrapContentWith(div);
        }
    }
}
