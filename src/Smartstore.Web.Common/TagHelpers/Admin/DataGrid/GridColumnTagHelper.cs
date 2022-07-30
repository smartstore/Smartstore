using Humanizer;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("column", Attributes = ForAttributeName, ParentTag = "columns")]
    [RestrictChildren("display-template", "edit-template", "footer-template")]
    public class GridColumnTagHelper : TagHelper
    {
        const string ForAttributeName = "for";
        const string TitleAttributeName = "title";
        const string HintAttributeName = "hint";
        const string WidthAttributeName = "width";
        const string VisibleAttributeName = "visible";
        const string FlowAttributeName = "flow";
        const string HAlignAttributeName = "halign";
        const string VAlignAttributeName = "valign";
        const string TypeAttributeName = "type";
        const string FormatAttributeName = "format";
        const string ResizableAttributeName = "resizable";
        const string SortableAttributeName = "sortable";
        const string ReadonlyAttributeName = "readonly";
        const string FilterableAttributeName = "filterable";
        const string GroupableAttributeName = "groupable";
        const string ReorderableAttributeName = "reorderable";
        const string HideableAttributeName = "hideable";
        const string EncodedAttributeName = "encoded";
        const string WrapAttributeName = "wrap";
        const string EntityMemberAttributeName = "entity-member";
        const string IconAttributeName = "icon";
        const string DefaultValueAttributeName = "default-value";
        const string OnCellClassAttributeName = "oncellclass";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(GridColumnTagHelper)] = this;
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Columns.Add(this);
            }
        }

        /// <summary>
        /// The bound member.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        /// <summary>
        /// Overrides the auto-resolved column title. Use empty string to hide the column header label.
        /// </summary>
        [HtmlAttributeName(TitleAttributeName)]
        public string Title { get; set; }

        /// <summary>
        /// Sets the title tag of a column. Use it to display user hints.
        /// </summary>
        [HtmlAttributeName(HintAttributeName)]
        public string Hint { get; set; }

        /// <summary>
        /// Columns width. Any CSS grid width specification is valid.
        /// </summary>
        [HtmlAttributeName(WidthAttributeName)]
        public string Width { get; set; }

        /// <summary>
        /// Column initial visibility. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(VisibleAttributeName)]
        public bool Visible { get; set; } = true;

        ///// <summary>
        ///// Flow of cell content. Default: <see cref="FlexFlow.Row"/>.
        ///// </summary>
        //[HtmlAttributeName(FlowAttributeName)]
        //public FlexFlow? Flow { get; set; }

        /// <summary>
        /// Horizontal alignment of cell content. Any <c>justify-content</c> value is valid:
        /// flex-start (default) | center | flex-end | space-around | space-between | space-evenly | stretch
        /// </summary>
        [HtmlAttributeName(HAlignAttributeName)]
        public string HAlign { get; set; }

        /// <summary>
        /// Vertical alignment of cell content. Any <c>align-items</c> value is valid:
        /// flex-start | center (default) | flex-end | baseline | stretch
        /// </summary>
        [HtmlAttributeName(VAlignAttributeName)]
        public string VAlign { get; set; }

        /// <summary>
        /// Column display render type. Leave empty to auto-resolve based on model expression.
        /// </summary>
        [HtmlAttributeName(TypeAttributeName)]
        public string Type { get; set; }

        /// <summary>
        /// Column value format string, e.g. "{0:N2}", "{0:G}", "{0:L LT}", "Value {0}" etc. 
        /// <para>
        /// Valid modifiers for numbers are: D[0-n], N[0-n], C[0-n], P[0-n].
        /// </para>
        /// <para>
        /// Valid modifiers for datetimes are:
        /// </para>
        /// <para>
        /// Standard .NET-like format specifiers: D, F, G, M, T, Y, d, f, g, t, u
        /// </para>
        /// <para>
        /// Custom .NET datetime format specifiers for date parts, like e.g. d, ddd, HH, m, yyyy etc.
        /// </para>
        /// <para>
        /// Any valid moment.js format string, e.g. L, L LT, DD, YYYY etc.
        /// </para>
        /// </summary>
        [HtmlAttributeName(FormatAttributeName)]
        public string Format { get; set; }

        /// <summary>
        /// Allows resizing of column, but only if resizing is enabled on grid level. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(ResizableAttributeName)]
        public bool Resizable { get; set; } = true;

        /// <summary>
        /// Allows sorting of column, but only if sorting is enabled on grid level. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(SortableAttributeName)]
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Allows filtering of column, but only if filtering is enabled on grid level. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(FilterableAttributeName)]
        public bool Filterable { get; set; } = true;

        /// <summary>
        /// Allows grouping of column, but only if grouping is enabled on grid level. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(GroupableAttributeName)]
        public bool Groupable { get; set; } = true;

        /// <summary>
        /// Allows reordering of column, but only if reordering is enabled on grid level. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(ReorderableAttributeName)]
        public bool Reorderable { get; set; } = true;

        /// <summary>
        /// Allows hiding of column, but only if hiding is enabled on grid level. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(HideableAttributeName)]
        public bool Hideable { get; set; } = true;

        /// <summary>
        /// Makes column uneditable, even if editing is enabled on grid level. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(ReadonlyAttributeName)]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Allows cell content wrapping. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(WrapAttributeName)]
        public bool Wrap { get; set; }

        /// <summary>
        /// Html encodes cell value. Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(EncodedAttributeName)]
        public bool Encoded { get; set; }

        /// <summary>
        /// The entity member/property name. Use this if the corresponding
        /// entity member name differs, e.g. 'CreatedOn' --> 'CreatedOnUtc'.
        /// </summary>
        [HtmlAttributeName(EntityMemberAttributeName)]
        public string EntityMember { get; set; }

        /// <summary>
        /// CSS class name of icon to use in column header, e.g. 'far fa-envelope'.
        /// </summary>
        [HtmlAttributeName(IconAttributeName)]
        public string Icon { get; set; }

        /// <summary>
        /// Name of Javascript function to call for custom CSS class binding for a particular column cell (tbody > tr > td). 
        /// The function should return a plain object that can be used in a Vue <c>v-bind:class</c> directive.
        /// Function parameters: <c>this</c> = Grid component instance, <c>value</c>, <c>column</c>, <c>row</c>.
        /// </summary>
        [HtmlAttributeName(OnCellClassAttributeName)]
        public string OnCellClass { get; set; }

        [HtmlAttributeNotBound]
        public TagHelperContent DisplayTemplate { get; set; }

        [HtmlAttributeNotBound]
        public TagHelperContent EditTemplate { get; set; }

        [HtmlAttributeNotBound]
        public TagHelperContent FooterTemplate { get; set; }

        [HtmlAttributeNotBound]
        public string MemberName
        {
            get => For.Name;
        }

        [HtmlAttributeNotBound]
        public string NormalizedMemberName
        {
            get => MemberName.ToLowerInvariant();
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.GetChildContentAsync();
            output.SuppressOutput();
        }

        internal object ToPlainObject()
        {
            return new
            {
                member = MemberName,
                name = For.Metadata.DisplayName ?? For.Metadata.PropertyName.Titleize(),
                title = Title ?? For.Metadata.DisplayName ?? For.Metadata.PropertyName.Titleize(),
                hint = Hint,
                width = Width.EmptyNull(),
                visible = Visible,
                halign = HAlign,
                valign = VAlign,
                type = GetColumnType(),
                nullable = For.Metadata.IsNullableValueType,
                format = Format,
                resizable = Resizable,
                sortable = Sortable,
                filterable = Filterable,
                groupable = Groupable,
                hideable = Hideable,
                editable = !ReadOnly,
                reorderable = Reorderable,
                wrap = Wrap,
                encoded = Encoded,
                entityMember = EntityMember,
                icon = Icon,
                onCellClass = OnCellClass
            };
        }

        private string GetColumnType()
        {
            if (Type.HasValue())
            {
                return Type;
            }

            var t = For.Metadata.ModelType.GetNonNullableType();

            if (t == typeof(string) || typeof(IHtmlContent).IsAssignableFrom(t))
            {
                return "string";
            }
            if (t == typeof(bool))
            {
                return "boolean";
            }
            else if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            {
                return "date";
            }
            else if (t.IsNumericType())
            {
                if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
                {
                    return "float";
                }

                return "int";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Custom display template for the cell content as Vue slot template. Passed object provides following members:
    /// <code>
    /// {
    ///     options,
    ///     dataSource,
    ///     columns,
    ///     paging,
    ///     sorting,
    ///     filtering,
    ///     item: {
    ///         value,
    ///         row,
    ///         rowIndex, 
    ///         column,
    ///         columnIndex
    ///     }
    /// }
    /// </code>
    /// </summary>
    [HtmlTargetElement("display-template", ParentTag = "column")]
    public class DisplayTemplateTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context.Items.TryGetValue(nameof(GridColumnTagHelper), out var obj) && obj is GridColumnTagHelper column)
            {
                column.DisplayTemplate = new DefaultTagHelperContent();
                (await output.GetChildContentAsync()).CopyTo(column.DisplayTemplate);
            }
        }
    }

    [HtmlTargetElement("edit-template", ParentTag = "column")]
    public class EditTemplateTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context.Items.TryGetValue(nameof(GridColumnTagHelper), out var obj) && obj is GridColumnTagHelper column)
            {
                column.EditTemplate = new DefaultTagHelperContent();
                (await output.GetChildContentAsync()).CopyTo(column.EditTemplate);
            }
        }
    }

    /// <summary>
    /// Custom footer template for column as Vue slot template. Use this to render aggregate values
    /// passed to <see cref="Smartstore.Web.Models.DataGrid.IGridModel.Aggregates"/>.
    /// Passed object provides following members:
    /// <code>
    /// {
    ///     column,
    ///     columnIndex,
    ///     aggregates
    /// }
    /// </code>
    /// </summary>
    [HtmlTargetElement("footer-template", ParentTag = "column")]
    public class FooterTemplateTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context.Items.TryGetValue(nameof(GridColumnTagHelper), out var obj) && obj is GridColumnTagHelper column)
            {
                column.FooterTemplate = new DefaultTagHelperContent();
                (await output.GetChildContentAsync()).CopyTo(column.FooterTemplate);
            }
        }
    }
}
