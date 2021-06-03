using System;
using System.Threading.Tasks;
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
    /// Template for the toolbar content as Vue slot template. Root object is called <c>grid</c>
    /// and provides the following members: <c>selectedRows, selectedRowsCount, selectedRowKeys, hasSelection, hasSearchPanel, showSearch, command, rows, editing, submitChanges(), cancelEdit(), deleteSelected()</c>
    /// </summary>
    [HtmlTargetElement("toolbar", ParentTag = "datagrid")]
    [RestrictChildren("toolbar-group")]
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

    [OutputElementHint("div")]
    [HtmlTargetElement("toolbar-group", ParentTag = "toolbar")]
    public class GridToolbarGroupTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AppendCssClass("dg-toolbar-group");
        }
    }

    [HtmlTargetElement("a", ParentTag = "toolbar-group")]
    [HtmlTargetElement("button", ParentTag = "toolbar-group")]
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

            if (Action == DataGridToolAction.ToggleSearchPanel)
            {
                output.AppendCssClass("dg-search-toggle");
                output.MergeAttribute("v-if", "grid.hasSearchPanel");
                output.MergeAttribute("v-bind:class", "{ 'active': grid.showSearch }");
                output.MergeAttribute("v-on:click", "grid.toggleSearch");
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
                output.PostContent.AppendHtml("<span v-if='grid.hasSelection' class='fwm'>({{ grid.selectedRowsCount }})</span>");
            }

            if (Action == DataGridToolAction.DeleteSelectedRows)
            {
                output.MergeAttribute("v-on:click.prevent", "grid.deleteSelected");
            }

            // TODO: (core) Add more actions
        }
    }
}
