using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("tab-content-header", ParentTag = "tabstrip")]
    public class TabContentHeaderTagHelper : SmartTagHelper
    {
        [HtmlAttributeNotBound]
        internal TagHelperContent Content { get; set; }

        [HtmlAttributeNotBound]
        internal TagHelperAttributeList Attributes { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);

            if (context.Items.TryGetValue(nameof(TabStripTagHelper), out var obj) && obj is TabStripTagHelper parent)
            {
                parent.TabContentHeader = this;
            }
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            Content = await output.GetChildContentAsync();

            // Remember tab item attributes so we can copy them later.
            Attributes = new TagHelperAttributeList(output.Attributes);

            output.TagName = null;
            output.SuppressOutput();
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}