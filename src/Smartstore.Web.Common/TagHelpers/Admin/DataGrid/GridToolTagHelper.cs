using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    public enum DataGridToolAction
    {
        AddRow,
        CancelEdit,
        SubmitChanges,
        DeleteSelectedRows,
        ReactToSelection
    }
    
    [HtmlTargetElement("a", ParentTag = "toolbar")]
    [HtmlTargetElement("button", ParentTag = "toolbar")]
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
            
            if (Action == DataGridToolAction.DeleteSelectedRows || Action == DataGridToolAction.ReactToSelection)
            {
                output.MergeAttribute("v-bind:class", "{ 'disabled': !grid.hasSelection }");
                output.PostContent.AppendHtml("<span v-if='grid.hasSelection' class='fwm'>({{ grid.selectedRowsCount }})</span>");
            }

            if (Action == DataGridToolAction.DeleteSelectedRows)
            {
                output.MergeAttribute("v-on:click", "grid.deleteSelected");
            }

            // TODO: (core) Add more actions
        }
    }
}
