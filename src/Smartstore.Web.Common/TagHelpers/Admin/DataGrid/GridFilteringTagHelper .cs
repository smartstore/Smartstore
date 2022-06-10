using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Rules;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("filtering", ParentTag = "datagrid")]
    [RestrictChildren("filter")]
    public class GridFilteringTagHelper : TagHelper
    {
        const string EnabledAttributeName = "enabled";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Filtering = this;
            }
        }

        [HtmlAttributeName(EnabledAttributeName)]
        public bool Enabled { get; set; } = true;

        [HtmlAttributeNotBound]
        internal List<GridFilterTagHelper> Descriptors { get; set; } = new();

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
                descriptors = command == null
                    ? Descriptors.Select(x => x.ToPlainObject()).ToArray()
                    : Descriptors.Select(x => x.ToPlainObject()).ToArray() // TODO: (core) Implement command filter preservation
            };
        }
    }

    [HtmlTargetElement("filter", Attributes = "for,op", ParentTag = "filtering", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class GridFilterTagHelper : TagHelper
    {
        const string ForAttributeName = "for";
        const string OperatorAttributeName = "op";
        const string ValueAttributeName = "value";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(GridTagHelper), out var obj) && obj is GridTagHelper parent)
            {
                parent.Filtering.Descriptors.Add(this);
            }
        }

        /// <summary>
        /// The filtered member.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeName(OperatorAttributeName)]
        public RuleOperator Operator { get; set; }

        [HtmlAttributeName(ValueAttributeName)]
        public object Value { get; set; }

        [HtmlAttributeNotBound]
        internal string MemberName
        {
            get => For.Metadata.Name;
        }

        [HtmlAttributeNotBound]
        internal Type MemberType
        {
            get => For.Metadata.ModelType;
        }

        internal object ToPlainObject()
        {
            return new { member = MemberName, op = Operator.ToString(), value = Value };
        }
    }
}
