using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("datasource", ParentTag = "datagrid", Attributes = "read", TagStructure = TagStructure.WithoutEndTag)]
    public class GridDataSourceTagHelper : TagHelper
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
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

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();
        }

        internal object ToPlainObject()
        {
            return new
            {
                read = Read,
                insert = Insert,
                update = Update,
                del = Delete
            };
        }
    }
}
