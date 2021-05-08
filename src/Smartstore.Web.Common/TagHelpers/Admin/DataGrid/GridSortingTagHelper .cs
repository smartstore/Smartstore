using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("sorting", ParentTag = "datagrid")]
    [RestrictChildren("sort")]
    public class GridSortingTagHelper : TagHelper
    {
        const string EnabledAttributeName = "enabled";
        const string AllowUnsortAttributeName = "allow-unsort";
        const string MultiSortAttributeName = "allow-multisort";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Sorting = this;
            }
        }

        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Default: <c>true</c>.
        /// </summary>
        [HtmlAttributeName(AllowUnsortAttributeName)]
        public bool AllowUnsort { get; set; } = true;

        /// <summary>
        /// Default: <c>false</c>.
        /// </summary>
        [HtmlAttributeName(MultiSortAttributeName)]
        public bool MultiSort { get; set; }

        [HtmlAttributeNotBound]
        internal List<GridSortTagHelper> Descriptors { get; set; } = new();

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.GetChildContentAsync();
            output.SuppressOutput();
        }
    }

    [HtmlTargetElement("sort", Attributes = ForAttributeName, ParentTag = "sorting", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class GridSortTagHelper : TagHelper
    {
        const string ForAttributeName = "by";
        const string DescendingAttributeName = "descending";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Sorting.Descriptors.Add(this);
            }
        }

        /// <summary>
        /// The sorted member.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression By { get; set; }

        [HtmlAttributeName(DescendingAttributeName)]
        public bool Descending { get; set; }

        [HtmlAttributeNotBound]
        internal string MemberName
        {
            get => By.Metadata.Name;
        }
    }
}
