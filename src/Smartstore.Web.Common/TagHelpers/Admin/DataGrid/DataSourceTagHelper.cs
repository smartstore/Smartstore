using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("datasource", ParentTag = "datagrid", Attributes = "read", TagStructure = TagStructure.WithoutEndTag)]
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

        /// <summary>
        /// The URL to read data from.
        /// </summary>
        public string Read { get; set; }

        /// <summary>
        /// The URL to post new data to.
        /// </summary>
        public string Insert { get; set; }

        /// <summary>
        /// The URL to post updated data to.
        /// </summary>
        public string Update { get; set; }

        /// <summary>
        /// The URL to delete data from.
        /// </summary>
        public string Delete { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
            => output.SuppressOutput();

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
