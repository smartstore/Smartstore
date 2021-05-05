using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Smartstore.ComponentModel;

namespace Smartstore.Web.TagHelpers.Admin
{
    [Flags]
    public enum DataGridBorderStyle
    {
        Borderless = 0,
        VerticalBorders = 1 << 0,
        HorizontalBorders = 1 << 1,
        Grid = VerticalBorders | HorizontalBorders
    }
    
    [HtmlTargetElement("datagrid")]
    [RestrictChildren("columns", "datasource", "pageable", "toolbar")]
    public class DataGridTagHelper : SmartTagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(DataGridTagHelper)] = this;
        }

        /// <summary>
        /// DataGrid table border style. Default: <see cref="DataGridBorderStyle.VerticalBorders"/>.
        /// </summary>
        public DataGridBorderStyle BorderStyle { get; set; } = DataGridBorderStyle.VerticalBorders;

        [HtmlAttributeNotBound]
        internal DataSourceTagHelper DataSource { get; set; }

        [HtmlAttributeNotBound]
        internal PageableTagHelper Pageable { get; set; }

        [HtmlAttributeNotBound]
        internal ToolbarTagHelper Toolbar { get; set; }

        [HtmlAttributeNotBound]
        internal List<ColumnTagHelper> Columns { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.LoadAndSetChildContentAsync();

            output.TagName = "div";
            output.AppendCssClass("datagrid-root");

            var component = new TagBuilder("sm-data-grid");
            component.Attributes[":data-source"] = "dataSource";
            component.Attributes[":columns"] = "columns";
            component.Attributes[":command"] = "command";
            component.Attributes[":vborders"] = "vborders";
            component.Attributes[":hborders"] = "hborders";

            // Generate template slots
            foreach (var column in Columns)
            {
                if (column.DisplayTemplate?.IsEmptyOrWhiteSpace == false)
                {
                    var displaySlot = new TagBuilder("template");
                    displaySlot.Attributes["v-slot:display-" + column.NormalizedMemberName] = "cell";
                    displaySlot.InnerHtml.AppendHtml(column.DisplayTemplate);
                    component.InnerHtml.AppendHtml(displaySlot);
                }

                if (column.EditTemplate?.IsEmptyOrWhiteSpace == false)
                {
                    //
                }
            }

            output.Content.AppendHtml(component);

            output.PostElement.AppendHtmlLine(
@$"<script>
	$(function() {{ new Vue({GenerateVueJson()}); }})
</script>");
        }

        private string GenerateVueJson()
        {
            var dict = new Dictionary<string, object>
            {
                { "el", "#" + Id }
            };

            var data = new
            {
                dataSource = new
                {
                    read = DataSource.Read,
                    insert = DataSource.Insert,
                    update = DataSource.Update,
                    delete = DataSource.Delete
                },
                columns = new List<object>(Columns.Count),
                command = new
                {
                    page = 1,
                    pageSize = 25
                },
                vborders = BorderStyle.HasFlag(DataGridBorderStyle.VerticalBorders),
                hborders = BorderStyle.HasFlag(DataGridBorderStyle.HorizontalBorders)
            };

            foreach (var col in Columns)
            {
                data.columns.Add(new 
                {
                    member = col.MemberName,
                    title = col.For.Metadata.DisplayName,
                    width = col.Width,
                    flow = col.Flow?.ToString()?.ToLower(),
                    align = col.AlignItems?.ToString()?.Kebaberize(),
                    justify = col.JustifyContent?.ToString()?.Kebaberize(),
                });
            }

            dict["data"] = data;

            var json = JsonConvert.SerializeObject(dict, new JsonSerializerSettings
            {
                ContractResolver = SmartContractResolver.Instance,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });

            return json;
        }

        //protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        //{
        //    await output.LoadAndSetChildContentAsync();

        //    output.TagName = "table";
        //    output.AppendCssClass("table admin-table");

        //    var tr = new TagBuilder("tr");

        //    foreach (var col in Columns)
        //    {
        //        tr.InnerHtml.AppendHtml(GenerateHeadCell(col));
        //    }

        //    var thead = new TagBuilder("thead");
        //    thead.InnerHtml.AppendHtml(tr);

        //    output.Content.AppendHtml(thead);
        //}

        //private IHtmlContent GenerateHeadCell(ColumnTagHelper col)
        //{
        //    var th = new TagBuilder("th");
        //    th.Attributes["data-field"] = col.For.Name;

        //    th.InnerHtml.Append(col.For.Metadata.DisplayName);
        //    return th;
        //}
    }
}
