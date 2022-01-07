using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    public enum DataGridToolAction
    {
        InsertRow,
        CancelEdit,
        SaveChanges,
        DeleteSelectedRows,
        ReactToSelection,
        ToggleSearchPanel
    }

    /// <summary>
    /// Template for the toolbar content as Vue slot template. Passed object provides following members:
    /// <code>
    /// {
    ///     options,
    ///     dataSource,
    ///     columns,
    ///     paging,
    ///     sorting,
    ///     filtering,
    ///     grid: {
    ///         selectedRows,
    ///         selectedRowsCount,
    ///         selectedRowKeys, 
    ///         hasSelection,
    ///         hasSearchPanel,
    ///         numSearchFilters,
    ///         command,
    ///         rows,
    ///         editing,
    ///         insertRow(),
    ///         saveChanges(),
    ///         cancelEdit(),
    ///         deleteSelectedRows(),
    ///         resetState()
    ///     }
    /// }
    /// </code>
    /// </summary>
    [HtmlTargetElement("toolbar", ParentTag = "datagrid")]
    [RestrictChildren("toolbar-group", "a", "button", "div", "zone")]
    public class GridToolbarTagHelper : TagHelper
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
            output.Attributes.Add("v-slot:toolbar", "grid");

            var div = new TagBuilder("div");
            div.Attributes.Add("class", "dg-toolbar d-flex flex-nowrap");

            output.WrapContentWith(div);
        }
    }

    [OutputElementHint("div")]
    [HtmlTargetElement("toolbar-group", ParentTag = "toolbar")]
    [RestrictChildren("a", "button", "div", "zone")]
    public class GridToolbarGroupTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AppendCssClass("dg-toolbar-group");
        }
    }

    [HtmlTargetElement("a", Attributes = ActionAttributeName, ParentTag = "toolbar")]
    [HtmlTargetElement("a", Attributes = ActionAttributeName, ParentTag = "toolbar-group")]
    [HtmlTargetElement("button", Attributes = ActionAttributeName, ParentTag = "toolbar")]
    [HtmlTargetElement("button", Attributes = ActionAttributeName, ParentTag = "toolbar-group")]
    public class GridToolTagHelper : TagHelper
    {
        const string ActionAttributeName = "datagrid-action";

        [HtmlAttributeName(ActionAttributeName)]
        public DataGridToolAction? Action { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (Action == null)
            {
                return;
            }

            output.MergeAttribute("href", "#");

            if (Action == DataGridToolAction.ToggleSearchPanel)
            {
                output.AppendCssClass("dg-search-toggle");
                //output.MergeAttribute("v-if", "grid.hasSearchPanel"); // ??? Hmmm...
                output.MergeAttribute("v-bind:class", "{ 'active': options.showSearch }");
                output.MergeAttribute("v-on:click", "options.showSearch = !options.showSearch");
                output.PostContent.AppendHtml("<span v-if='grid.numSearchFilters > 0' class='badge badge-pill badge-success dg-toolbar-badge'>{{ grid.numSearchFilters }}</span>");

                return;
            }

            if (Action == DataGridToolAction.InsertRow)
            {
                output.MergeAttribute("v-if", "!grid.editing.active");
                output.MergeAttribute("v-on:click.prevent", "grid.insertRow");
            }

            if (Action == DataGridToolAction.SaveChanges || Action == DataGridToolAction.CancelEdit)
            {
                output.MergeAttribute("v-if", "grid.editing.active");
            }

            if (Action == DataGridToolAction.SaveChanges)
            {
                output.MergeAttribute("v-on:click.prevent", "grid.saveChanges");
            }

            if (Action == DataGridToolAction.CancelEdit)
            {
                output.MergeAttribute("v-on:click.prevent", "grid.cancelEdit");
            }

            if (Action == DataGridToolAction.DeleteSelectedRows || Action == DataGridToolAction.ReactToSelection)
            {
                output.MergeAttribute("v-bind:class", "{ 'disabled': !grid.hasSelection }");
                output.PostContent.AppendHtml("<span v-if='grid.hasSelection' class='badge badge-success'>{{ grid.selectedRowsCount }}</span>");
            }

            if (Action == DataGridToolAction.DeleteSelectedRows)
            {
                output.MergeAttribute("v-on:click.prevent", "grid.deleteSelectedRows");
            }
        }
    }
}
