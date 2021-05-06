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
        const string BorderAttributeName = "border-style";
        const string StripedAttributeName = "striped";
        const string HoverAttributeName = "hover";
        const string CondensedAttributeName = "condensed";
        const string AllowResizeAttributeName = "allow-resize";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(DataGridTagHelper)] = this;
        }

        /// <summary>
        /// DataGrid table border style. Default: <see cref="DataGridBorderStyle.VerticalBorders"/>.
        /// </summary>
        [HtmlAttributeName(BorderAttributeName)]
        public DataGridBorderStyle BorderStyle { get; set; } = DataGridBorderStyle.VerticalBorders;

        /// <summary>
        /// Adds zebra-striping to any table row within tbody.
        /// </summary>
        [HtmlAttributeName(StripedAttributeName)]
        public bool Striped { get; set; }

        /// <summary>
        /// Enables a hover state on table rows within tbody.
        /// </summary>
        [HtmlAttributeName(HoverAttributeName)]
        public bool Hover { get; set; }

        /// <summary>
        /// Makes data table more compact by cutting cell padding in half.
        /// </summary>
        [HtmlAttributeName(CondensedAttributeName)]
        public bool Condensed { get; set; }

        /// <summary>
        /// Allows resizing of single columns. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(AllowResizeAttributeName)]
        public bool AllowResize { get; set; }

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
            component.Attributes[":options"] = "options";

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

            output.PostElement.AppendHtmlLine(@$"<script>$(function() {{ window['{Id}'] = new Vue({GenerateVueJson()}); }})</script>");
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
                options = new
                {
                    vborders = BorderStyle.HasFlag(DataGridBorderStyle.VerticalBorders),
                    hborders = BorderStyle.HasFlag(DataGridBorderStyle.HorizontalBorders),
                    striped = Striped,
                    hover = Hover,
                    condensed = Condensed
                }
            };

            foreach (var col in Columns)
            {
                data.columns.Add(new 
                {
                    member = col.MemberName,
                    title = col.For.Metadata.DisplayName,
                    width = col.Width.EmptyNull(),
                    visible = col.Visible,
                    flow = col.Flow?.ToString()?.ToLower(),
                    align = col.AlignItems?.ToString()?.Kebaberize(),
                    justify = col.JustifyContent?.ToString()?.Kebaberize(),
                    type = GetColumnType(col),
                    format = col.Format,
                    resizable = AllowResize && col.Resizable
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

        private static string GetColumnType(ColumnTagHelper column)
        {
            if (column.Type.HasValue())
            {
                return column.Type;
            }

            var xxx = column.For.Metadata.DataTypeName;
            var modelType = column.For.Metadata.ModelType.GetNonNullableType();

            if (modelType == typeof(bool))
            {
                return "bool";
            }
            else if (modelType == typeof(DateTime))
            {
                return "datetime";
            }
            else if (modelType.IsNumericType())
            {
                return "number";
            }

            return null;
        }
    }
}
