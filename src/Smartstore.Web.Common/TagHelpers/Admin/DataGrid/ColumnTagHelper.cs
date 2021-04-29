using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("column", ParentTag = "columns")]
    //[RestrictChildren("tool")]
    public class ColumnTagHelper : SmartTagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(DataGridTagHelper), out var obj) && obj is DataGridTagHelper parent)
            {
                parent.Columns.Add(this);
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
