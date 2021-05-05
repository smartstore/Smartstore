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
    public class ColumnTagHelper : SmartTagHelper
    {
        const string ForAttributeName = "for";
        const string WidthAttributeName = "width";
        const string FlowAttributeName = "flow";
        const string AlignAttributeName = "align-items";
        const string JustifyAttributeName = "justify";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(ColumnTagHelper)] = this;
            if (context.Items.TryGetValue(nameof(DataGridTagHelper), out var obj) && obj is DataGridTagHelper parent)
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
        /// Columns width. Any CSS grid width specification is valid.
        /// </summary>
        [HtmlAttributeName(WidthAttributeName)]
        public string Width { get; set; }

        /// <summary>
        /// Flow of cell content. Default: <see cref="FlexFlow.Row"/>.
        /// </summary>
        [HtmlAttributeName(FlowAttributeName)]
        public FlexFlow? Flow { get; set; }

        /// <summary>
        /// Alignment of cell content. If <see cref="Flow"/> is <see cref="FlexFlow.Column"/>, 
        /// this is the horizontal alignment, otherwise vertical.
        /// Default: <see cref="FlexAlignItems.Center"/>.
        /// </summary>
        [HtmlAttributeName(AlignAttributeName)]
        public FlexAlignItems? AlignItems { get; set; }

        /// <summary>
        /// Justification of cell content. If <see cref="Flow"/> is <see cref="FlexFlow.Column"/>, 
        /// this is the vertical alignment, otherwise horizontal.
        /// Default: <see cref="FlexJustifyContent.FlexStart"/>.
        /// </summary>
        [HtmlAttributeName(JustifyAttributeName)]
        public FlexJustifyContent? JustifyContent { get; set; }

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

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.GetChildContentAsync();
            output.SuppressOutput();
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
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
            if (context.Items.TryGetValue(nameof(ColumnTagHelper), out var obj) && obj is ColumnTagHelper column)
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
            if (context.Items.TryGetValue(nameof(ColumnTagHelper), out var obj) && obj is ColumnTagHelper column)
            {
                //column.EditTemplate = await output.GetChildContentAsync();
                column.EditTemplate = new DefaultTagHelperContent();
                (await output.GetChildContentAsync()).CopyTo(column.EditTemplate);
            }
        }
    }
}
