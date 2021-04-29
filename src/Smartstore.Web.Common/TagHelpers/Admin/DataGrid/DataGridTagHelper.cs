using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

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

            output.SuppressOutput();
        }
    }
}
