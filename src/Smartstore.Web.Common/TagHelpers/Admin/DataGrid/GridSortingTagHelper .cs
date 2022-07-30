using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Models.DataGrid;

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

        internal object ToPlainObject(GridCommand command = null)
        {
            return new
            {
                enabled = Enabled,
                allowUnsort = AllowUnsort,
                allowMultiSort = MultiSort,
                descriptors = command == null
                    ? Descriptors.Select(x => x.ToPlainObject()).ToArray()
                    : command.Sorting.Select(x => new { member = x.Member, descending = x.Descending }).ToArray()
            };
        }
    }

    [HtmlTargetElement("sort", Attributes = ByAttributeName, ParentTag = "sorting", TagStructure = TagStructure.NormalOrSelfClosing)]
    [HtmlTargetElement("sort", Attributes = ByMemberAttributeName, ParentTag = "sorting", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class GridSortTagHelper : TagHelper
    {
        const string ByAttributeName = "by";
        const string ByMemberAttributeName = "by-entity-member";
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
        [HtmlAttributeName(ByAttributeName)]
        public ModelExpression By { get; set; }

        /// <summary>
        /// The sorted entity member name.
        /// </summary>
        [HtmlAttributeName(ByMemberAttributeName)]
        public string ByMember { get; set; }

        [HtmlAttributeName(DescendingAttributeName)]
        public bool Descending { get; set; }

        [HtmlAttributeNotBound]
        internal string MemberName
        {
            get => ByMember.NullEmpty() ?? By?.Metadata?.Name;
        }

        internal object ToPlainObject()
        {
            return new { member = MemberName, descending = Descending };
        }
    }
}
