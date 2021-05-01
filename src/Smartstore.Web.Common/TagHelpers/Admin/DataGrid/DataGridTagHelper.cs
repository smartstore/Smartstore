using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Smartstore.ComponentModel;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("datagrid")]
    [RestrictChildren("columns", "datasource", "pageable", "toolbar")]
    public class DataGridTagHelper : SmartTagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(DataGridTagHelper)] = this;
        }

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
                columns = new List<object>(Columns.Count)
            };

            foreach (var col in Columns)
            {
                data.columns.Add(new 
                {
                    field = col.For.Name,
                    name = col.For.Metadata.DisplayName,
                    width = col.Width
                });
            }

            dict["data"] = data;

            var json = JsonConvert.SerializeObject(dict, new JsonSerializerSettings
            {
                ContractResolver = SmartContractResolver.Instance,
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
