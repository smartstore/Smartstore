using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Smartstore.Utilities;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

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
    [RestrictChildren("columns", "datasource", "paging", "toolbar", "sorting", "search-panel", "row-commands", "detail-view")]
    public class GridTagHelper : SmartTagHelper
    {
        const string LoaderHtml = "<div class='datagrid-loader spinner-container w-100 h-100 active'><div class='spinner'><svg style='width:64px; height:64px' viewBox='0 0 64 64'><circle class='circle' cx='32' cy='32' r='30' fill='none' stroke-width='2'></circle></svg></div></div>";

        const string BorderAttributeName = "border-style";
        const string StripedAttributeName = "striped";
        const string HoverAttributeName = "hover";
        const string CondensedAttributeName = "condensed";
        const string AllowResizeAttributeName = "allow-resize";
        const string AllowRowSelectionAttributeName = "allow-row-selection";
        const string AllowColumnReorderingAttributeName = "allow-column-reordering";
        const string AllowEditAttributeName = "allow-edit";
        const string HideHeaderAttributeName = "hide-header";
        const string StickyFooterAttributeName = "sticky-footer";
        const string KeyMemberAttributeName = "key-member";
        const string MaxHeightAttributeName = "max-height";
        const string PreserveCommandStateAttributeName = "preserve-command-state";
        const string PreserveGridStateAttributeName = "preserve-grid-state";
        const string ClassAttributeName = "class";
        const string VersionAttributeName = "version";
        const string OnDataBindingAttributeName = "ondatabinding";
        const string OnDataBoundAttributeName = "ondatabound";
        const string OnRowSelectedAttributeName = "onrowselected";
        const string OnRowClassAttributeName = "onrowclass";
        const string OnCellClassAttributeName = "oncellclass";

        private readonly IGridCommandStateStore _gridCommandStateStore;
        private readonly IAntiforgery _antiforgery;

        public GridTagHelper(IGridCommandStateStore gridCommandStateStore, IAntiforgery antiforgery)
        {
            _gridCommandStateStore = gridCommandStateStore;
            _antiforgery = antiforgery;
        }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(GridTagHelper)] = this;
        }

        #region Public properties

        /// <summary>
        /// DataGrid table border style. Default: <see cref="DataGridBorderStyle.VerticalBorders"/>.
        /// </summary>
        [HtmlAttributeName(BorderAttributeName)]
        public DataGridBorderStyle BorderStyle { get; set; } = DataGridBorderStyle.VerticalBorders;

        /// <summary>
        /// Adds zebra-striping to any table row within tbody. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(StripedAttributeName)]
        public bool Striped { get; set; } = true;

        /// <summary>
        /// Enables a hover state on table rows within tbody.  Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(HoverAttributeName)]
        public bool Hover { get; set; } = true;

        ///// <summary>
        ///// Makes data table more compact by cutting cell padding in half.
        ///// </summary>
        //[HtmlAttributeName(CondensedAttributeName)]
        //public bool Condensed { get; set; }

        /// <summary>
        /// Allows resizing of single columns. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(AllowResizeAttributeName)]
        public bool AllowResize { get; set; }

        /// <summary>
        /// Allows selection of rows via pinned checkboxes on the left side. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(AllowRowSelectionAttributeName)]
        public bool AllowRowSelection { get; set; }

        /// <summary>
        /// Allows reordering of columns via drag & drop. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(AllowColumnReorderingAttributeName)]
        public bool AllowColumnReordering { get; set; }

        /// <summary>
        /// Allows inline editing of rows via double click. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(AllowEditAttributeName)]
        public bool AllowEdit { get; set; }

        /// <summary>
        /// Whether to hide data table header. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(HideHeaderAttributeName)]
        public bool HideHeader { get; set; }

        /// <summary>
        /// Whether to fix grid footer position while scrolling. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(StickyFooterAttributeName)]
        public bool StickyFooter { get; set; } = true;

        /// <summary>
        /// Key member expression. If <c>null</c>, any property named <c>Id</c> will be key member.
        /// </summary>
        [HtmlAttributeName(KeyMemberAttributeName)]
        public ModelExpression KeyMember { get; set; }

        /// <summary>
        /// Maximum height of grid element including toolbar, pager, header etc. Expressed as CSS max-height expression. Default: <c>initial</c>.
        /// </summary>
        [HtmlAttributeName(MaxHeightAttributeName)]
        public string MaxHeight { get; set; }

        /// <summary>
        /// Grid configuration version. This version is compared with the user preferences version
        /// saved in browser's localStorage. If this version differs, no attempt is made to load
        /// client preferences.
        /// Increment the value if you made changes to the grid columns or any user-customizable option.
        /// </summary>
        [HtmlAttributeName(VersionAttributeName)]
        public int Version { get; set; } = 1;

        /// <summary>
        /// Custom CSS classes to apply to generated root element .datagrid-root.
        /// </summary>
        [HtmlAttributeName(ClassAttributeName)]
        public string CssClass { get; set; }

        /// <summary>
        /// Preserves command state of data grid across requests, but only within current session. 
        /// The state key is varied by current route identifier / URL.
        /// Data is saved on the server side. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(PreserveCommandStateAttributeName)]
        public bool PreserveCommandState { get; set; } = true;

        /// <summary>
        /// Preserves local state of data grid across requests. 
        /// Data is saved on the client side (localStorage). Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(PreserveGridStateAttributeName)]
        public bool PreserveGridState { get; set; } = true;

        /// <summary>
        /// Name of Javascript function to call before data binding.
        /// Function parameters: <c>this</c> = Grid component instance, <c>command</c>.
        /// </summary>
        [HtmlAttributeName(OnDataBindingAttributeName)]
        public string OnDataBinding { get; set; }

        /// <summary>
        /// Name of Javascript function to call after data has been bound successfully.
        /// Function parameters: <c>this</c> = Grid component instance, <c>command</c>, <c>rows</c>.
        /// </summary>
        [HtmlAttributeName(OnDataBoundAttributeName)]
        public string OnDataBound { get; set; }

        /// <summary>
        /// Name of Javascript function to call after a rows has been (un)selected.
        /// Function parameters: <c>this</c> = Grid component instance, <c>selectedRows</c>, <c>row</c>, <c>selected</c>.
        /// </summary>
        [HtmlAttributeName(OnRowSelectedAttributeName)]
        public string OnRowSelected { get; set; }

        /// <summary>
        /// Name of Javascript function to call for custom CSS class binding on row level (tbody > tr). 
        /// The function should return a plain object that can be used in a Vue <c>v-bind:class</c> directive.
        /// Function parameters: <c>this</c> = Grid component instance, <c>row</c>.
        /// </summary>
        [HtmlAttributeName(OnRowClassAttributeName)]
        public string OnRowClass { get; set; }

        /// <summary>
        /// Name of Javascript function to call for custom global CSS class binding on cell level (tbody > tr > td). 
        /// The function should return a plain object that can be used in a Vue <c>v-bind:class</c> directive.
        /// Function parameters: <c>this</c> = Grid component instance, <c>value</c>, <c>column</c>, <c>row</c>.
        /// </summary>
        [HtmlAttributeName(OnCellClassAttributeName)]
        public string OnCellClass { get; set; }

        #endregion

        #region Internal properties

        [HtmlAttributeNotBound]
        internal GridDataSourceTagHelper DataSource { get; set; }

        [HtmlAttributeNotBound]
        internal GridPagingTagHelper Paging { get; set; }

        [HtmlAttributeNotBound]
        internal GridSortingTagHelper Sorting { get; set; }

        [HtmlAttributeNotBound]
        internal GridFilteringTagHelper Filtering { get; set; }

        [HtmlAttributeNotBound]
        internal List<GridColumnTagHelper> Columns { get; set; }

        [HtmlAttributeNotBound]
        internal GridSearchPanelTagHelper SearchPanel { get; set; }

        [HtmlAttributeNotBound]
        internal string KeyMemberName
        {
            get => KeyMember?.Metadata?.Name ?? "Id";
        }

        #endregion

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.LoadAndSetChildContentAsync();

            if (Columns == null || Columns.Count == 0)
            {
                throw new InvalidOperationException("At least one column must be specified in order for the grid to render correctly.");
            }

            var cssClass = "datagrid-root";
            if (CssClass.HasValue())
            {
                cssClass += " " + CssClass;
            }

            // Root wrapper div .datagrid-root
            output.PreElement.AppendHtml($"<div class='{cssClass}'>");

            // Append .datagrid-loader
            output.PostElement.AppendHtml(LoaderHtml);

            // Close .datagrid-root
            output.PostElement.AppendHtml("</div>");

            output.TagName = "sm-datagrid";
            output.Attributes.Add(":options", "options");
            output.Attributes.Add(":data-source", "dataSource");
            output.Attributes.Add(":columns", "columns");
            output.Attributes.Add(":paging", "paging");
            output.Attributes.Add(":sorting", "sorting");

            // Generate column template slots
            foreach (var column in Columns)
            {
                if (column.DisplayTemplate != null && !column.DisplayTemplate.IsEmptyOrWhiteSpace)
                {
                    var displaySlot = new TagBuilder("template");
                    displaySlot.Attributes["v-slot:display-" + column.NormalizedMemberName] = "item";
                    displaySlot.InnerHtml.AppendHtml(column.DisplayTemplate);
                    output.Content.AppendHtml(displaySlot);
                }

                if (AllowEdit && !column.ReadOnly)
                {
                    var editorSlot = new TagBuilder("template");
                    editorSlot.Attributes["v-slot:edit-" + column.NormalizedMemberName] = "item";

                    if (column.EditTemplate != null && !column.EditTemplate.IsEmptyOrWhiteSpace)
                    {
                        editorSlot.InnerHtml.AppendHtml(column.EditTemplate);
                    }
                    else
                    {
                        // No custom edit template specified
                        editorSlot.InnerHtml.AppendHtml(HtmlHelper.EditorFor(column.For));
                        //editorSlot.InnerHtml.AppendHtml(HtmlHelper.ValidationMessageFor(column.For));
                    }

                    output.Content.AppendHtml(editorSlot);
                }

                if (column.FooterTemplate != null && !column.FooterTemplate.IsEmptyOrWhiteSpace)
                {
                    var footerSlot = new TagBuilder("template");
                    footerSlot.Attributes["v-slot:colfooter-" + column.NormalizedMemberName] = "item";
                    footerSlot.InnerHtml.AppendHtml(column.FooterTemplate);
                    output.Content.AppendHtml(footerSlot);
                }
            }

            GridCommand preservedCommandState = PreserveCommandState ? await _gridCommandStateStore.LoadStateAsync(Id) : null;

            output.PostElement.AppendHtmlLine(@$"
<script>
    $(function() {{ 
        window.Res.DataGrid = {GenerateClientRes()};
        window['{Id}'] = new Vue({GenerateVueJson(preservedCommandState)}); 
    }})
</script>");
        }

        private string GenerateClientRes()
        {
            var resRoot = "Admin.DataGrid.";
            var clientRes = new Dictionary<string, string>
            {
                ["saveChanges"] = T("Admin.Common.SaveChanges"),
                ["filter"] = T("Common.Filter"),
                ["cancel"] = T("Common.Cancel"),
                ["resetState"] = T(resRoot + "ResetState"),
                ["fitColumns"] = T(resRoot + "FitColumns"),
                ["noData"] = T(resRoot + "NoData"),
                ["vborders"] = T(resRoot + "VBorders"),
                ["hborders"] = T(resRoot + "HBorders"),
                ["striped"] = T(resRoot + "Striped"),
                ["hover"] = T(resRoot + "Hover"),
                ["pagerPos"] = T(resRoot + "PagerPos"),
                ["pagerTop"] = T(resRoot + "PagerTop"),
                ["pagerBottom"] = T(resRoot + "PagerBottom"),
                ["pagerBoth"] = T(resRoot + "PagerBoth"),
                ["xPerPage"] = T(resRoot + "XPerPage"),
                ["displayingItems"] = T(resRoot + "DisplayingItems"),
                ["displayingItemsShort"] = T(resRoot + "DisplayingItemsShort"),
                ["confirmDelete"] = T(resRoot + "ConfirmDelete"),
                ["confirmDeleteMany"] = T(resRoot + "ConfirmDeleteMany"),
                ["deleteSuccess"] = T(resRoot + "DeleteSuccess"),
            };

            return SerializeObject(clientRes);
        }

        private string GenerateVueJson(GridCommand command)
        {
            var modelType = Columns.FirstOrDefault()?.For?.Metadata?.ContainerType;
            var defaultDataRow = modelType != null && modelType.HasDefaultConstructor()
                ? Activator.CreateInstance(modelType)
                : null;

            var pathChanged = command == null || !ViewContext.HttpContext.Request.RawUrl().EqualsNoCase(command.Path.EmptyNull());
            if (pathChanged)
            {
                command = null;
            }

            var dict = new Dictionary<string, object>
            {
                { "el", "#" + Id }
            };

            string antiforgeryToken = null;
            var isAjax = ViewContext.HttpContext.Request.IsAjax();
            if (!isAjax &&  (!ViewContext.FormContext.CanRenderAtEndOfForm || !ViewContext.FormContext.HasAntiforgeryToken))
            {
                var tokenSet = _antiforgery.GetTokens(ViewContext.HttpContext);
                antiforgeryToken = tokenSet.RequestToken;
            }

            var data = new
            {
                options = new
                {
                    vborders = BorderStyle.HasFlag(DataGridBorderStyle.VerticalBorders),
                    hborders = BorderStyle.HasFlag(DataGridBorderStyle.HorizontalBorders),
                    striped = Striped,
                    hover = Hover,
                    keyMemberName = KeyMemberName,
                    allowResize = AllowResize,
                    allowRowSelection = AllowRowSelection,
                    allowColumnReordering = AllowColumnReordering,
                    allowEdit = AllowEdit,
                    hideHeader = HideHeader,
                    stickyFooter = StickyFooter,
                    maxHeight = MaxHeight,
                    showSearch = false,
                    preserveSearchState = SearchPanel?.PreserveState ?? false,
                    stateKey = Id,
                    preserveState = PreserveGridState,
                    version = Version,
                    defaultDataRow,
                    onDataBinding = OnDataBinding,
                    onDataBound = OnDataBound,
                    onRowSelected = OnRowSelected,
                    onRowClass = OnRowClass,
                    onCellClass = OnCellClass,
                    antiforgeryToken
                },
                dataSource = DataSource?.ToPlainObject(),
                columns = Columns.Select(c => c.ToPlainObject()).ToList(),
                paging = Paging?.ToPlainObject(command) ?? new { },
                sorting = Sorting?.ToPlainObject(command) ?? new { },
                filtering = Filtering?.ToPlainObject(command) ?? new { },

                // Define reactive data properties required during (slot) rendering
                numSearchFilters = 0,
                dragging = new { active = false },
                editing = new { active = false }
            };

            dict["data"] = data;

            var json = SerializeObject(dict);

            return json;
        }

        private static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                Formatting = CommonHelper.IsDevEnvironment ? Formatting.Indented : Formatting.None
            });
        }
    }
}
