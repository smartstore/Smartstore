using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Common.Settings;
using Smartstore.Web.Models.DataGrid;

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
        const string ShowInfoAttributeName = "show-info";
        const string SizesAttributeName = "available-sizes";

        private readonly AdminAreaSettings _adminAreaSettings;
        private int _pageSize;
        private int[] _availableSizes;

        public GridPagingTagHelper(AdminAreaSettings adminAreaSettings)
        {
            _adminAreaSettings = adminAreaSettings;
            _pageSize = _adminAreaSettings.GridPageSize;
        }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Paging = this;
            }
        }

        /// <summary>
        /// Whether page number links should be rendered. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Page size. Default: <see cref="AdminAreaSettings.GridPageSize"/>, initially 25.
        /// </summary>
        [HtmlAttributeName(PageSizeAttributeName)]
        public int PageSize
        {
            get => _pageSize < 1 ? 25 : _pageSize;
            set => _pageSize = value;
        }

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

        /// <summary>
        /// Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(ShowChooserAttributeName)]
        public bool ShowSizeChooser { get; set; } = true;

        /// <summary>
        /// Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(ShowInfoAttributeName)]
        public bool ShowInfo { get; set; } = true;

        /// <summary>
        /// Available page sizes to choose from.
        /// </summary>
        [HtmlAttributeName(SizesAttributeName)]
        public int[] AvailableSizes
        {
            get => _availableSizes ??= new int[] { PageSize, PageSize * 2, PageSize * 4, PageSize * 6, PageSize * 8 };
            set => _availableSizes = value;
        }

        [HtmlAttributeName(TotalAttributeName)]
        public int? Total { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();
        }

        internal object ToPlainObject(GridCommand command = null)
        {
            return new
            {
                enabled = Enabled,
                pageSize = PageSize,
                pageIndex = command?.Page ?? PageIndex,
                position = Position.ToString().ToLower(),
                total = Total,
                showSizeChooser = ShowSizeChooser,
                showInfo = ShowInfo,
                availableSizes = AvailableSizes
            };
        }
    }
}
