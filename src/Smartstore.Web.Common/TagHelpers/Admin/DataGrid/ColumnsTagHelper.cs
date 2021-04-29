using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("columns", ParentTag = "datagrid")]
    [RestrictChildren("column")]
    public class ColumnsTagHelper : SmartTagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(DataGridTagHelper), out var obj) && obj is DataGridTagHelper parent)
            {
                parent.Columns = new List<ColumnTagHelper>();
            }
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.GetChildContentAsync();
            output.SuppressOutput();
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
