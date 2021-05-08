using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("column", Attributes = ForAttributeName, ParentTag = "columns")]
    [RestrictChildren("display-template", "edit-template")]
    public class GridColumnTagHelper : TagHelper
    {
        const string ForAttributeName = "for";
        const string TitleAttributeName = "title";
        const string WidthAttributeName = "width";
        const string VisibleAttributeName = "visible";
        const string FlowAttributeName = "flow";
        const string HAlignAttributeName = "halign";
        const string VAlignAttributeName = "valign";
        const string TypeAttributeName = "type";
        const string FormatAttributeName = "format";
        const string ResizableAttributeName = "resizable";
        const string SortableAttributeName = "sortable";
        const string NowrapAttributeName = "nowrap";
        const string EntityMemberAttributeName = "entity-member";

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
        /// Overrides the auto-resolved column title.
        /// </summary>
        [HtmlAttributeName(TitleAttributeName)]
        public string Title { get; set; }

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
        /// Column value format string. TODO: (core) Describe & samples
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
        /// Prevents cell content wrapping. Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(NowrapAttributeName)]
        public bool Nowrap { get; set; } = true;

        /// <summary>
        /// The entity member/property name. Use this if the corresponding
        /// entity member name differs, e.g. 'CreatedOn' --> 'CreatedOnUtc'.
        /// </summary>
        [HtmlAttributeName(EntityMemberAttributeName)]
        public string EntityMember { get; set; }

        [HtmlAttributeNotBound]
        public TagHelperContent DisplayTemplate { get; set; }

        [HtmlAttributeNotBound]
        public TagHelperContent EditTemplate { get; set; }

        [HtmlAttributeNotBound]
        public string MemberName 
        {
            get => For.Metadata.Name;
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
    }

    /// <summary>
    /// Custom display template for the cell content as Vue slot template. Root object is called <c>cell</c>
    /// and provides the following members:
    /// <list type="table">
    ///     <item><c>value</c>: the raw cell value</item>
    ///     <item><c>row</c></item>
    ///     <item><c>rowIndex</c></item>
    ///     <item><c>column</c></item>
    ///     <item><c>columnIndex</c></item>
    /// </list>
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
                //column.EditTemplate = await output.GetChildContentAsync();
                column.EditTemplate = new DefaultTagHelperContent();
                (await output.GetChildContentAsync()).CopyTo(column.EditTemplate);
            }
        }
    }
}
