using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.RowCommands = this;
            }
        }

        [HtmlAttributeNotBound]
        internal TagHelperContent Template { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();

            if (content.IsEmptyOrWhiteSpace)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "div";
            output.AppendCssClass("dg-commands-dropdown dropdown-menu dropdown-menu-right");
            output.Content.SetHtmlContent(content);

            Template = new DefaultTagHelperContent();
            output.CopyTo(Template);
            output.SuppressOutput();
        }
    }
}
