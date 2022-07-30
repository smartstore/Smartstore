using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    public enum DataRowAction
    {
        InlineEdit,
        Delete,
        Custom = 100
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

    [HtmlTargetElement("a", Attributes = ActionAttributeName, ParentTag = "row-commands")]
    public class GridRowCommandTagHelper : TagHelper
    {
        const string ActionAttributeName = "datarow-action";

        [HtmlAttributeName(ActionAttributeName)]
        public DataRowAction? Action { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Action == null)
            {
                return;
            }

            if (!output.Attributes.ContainsName(":href") && !output.Attributes.ContainsName("v-bind:href"))
            {
                output.MergeAttribute("href", "#");
            }

            output.AppendCssClass("dropdown-item");

            if (Action == DataRowAction.InlineEdit)
            {
                output.MergeAttribute("v-on:click.prevent", "item.activateEdit(item.row)");
            }
            else if (Action == DataRowAction.Delete)
            {
                output.MergeAttribute("v-on:click.prevent", "item.deleteRows(item.row)");
            }
        }
    }
}
