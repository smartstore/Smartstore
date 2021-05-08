using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    public enum GridPagerPosition
    {
        Bottom,
        Top,
        Both
    }
    
    [HtmlTargetElement("paging", ParentTag = "datagrid", TagStructure = TagStructure.WithoutEndTag)]
    public class GridPagingTagHelper : TagHelper
    {
        const string EnabledAttributeName = "enabled";
        const string PageSizeAttributeName = "page-size";
        const string PageIndexAttributeName = "page-index";
        const string PositionAttributeName = "position";
        const string TotalAttributeName = "total";
        const string ShowChooserAttributeName = "show-size-chooser";
        const string SizesAttributeName = "available-sizes";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Paging = this;
            }
        }

        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Page size. Default: 25.
        /// </summary>
        [HtmlAttributeName(PageSizeAttributeName)]
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// The 1-based current page index. Default: 1.
        /// </summary>
        [HtmlAttributeName(PageIndexAttributeName)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Pager panel position: Default: <see cref="GridPagerPosition.Bottom"/>.
        /// </summary>
        [HtmlAttributeName(PositionAttributeName)]
        public GridPagerPosition Position { get; set; }

        [HtmlAttributeName(ShowChooserAttributeName)]
        public bool ShowSizeChooser { get; set; }

        /// <summary>
        /// Available page sizes to choose from.
        /// </summary>
        [HtmlAttributeName(SizesAttributeName)]
        public int[] AvailableSizes { get; set; } = new int[] { 25, 50, 100, 200 };

        [HtmlAttributeName(TotalAttributeName)]
        public int? Total { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();
        }
    }
}
