using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("datasource", ParentTag = "datagrid", TagStructure = TagStructure.WithoutEndTag)]
    public class DataSourceTagHelper : SmartTagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(DataGridTagHelper), out var obj) && obj is DataGridTagHelper parent)
            {
                parent.DataSource = this;
            }
        }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
            => output.SuppressOutput();

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
