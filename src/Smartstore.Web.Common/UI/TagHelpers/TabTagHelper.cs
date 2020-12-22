using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    [OutputElementHint("a")]
    [HtmlTargetElement("tab", ParentTag = "tabstrip")]
    public class TabTagHelper : SmartTagHelper
    {
        /// <summary>
        /// Unique name of tab item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether item is in selected state.
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// Whether item is in disabled state.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Whether item is initially visible. Default = true.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Whether to load content deferred per AJAX.
        /// </summary>
        public bool Ajax { get; set; }

        [HtmlAttributeNotBound]
        internal TabStripTagHelper Parent { get; set; }

        [HtmlAttributeNotBound]
        internal int Index { get; set; }

        [HtmlAttributeNotBound]
        internal TagHelperContent Content { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            if (context.Items.TryGetValue(nameof(TabStripTagHelper), out var obj) && obj is TabStripTagHelper parent)
            {
                Parent = parent;
                Index = parent.Tabs.Count;
                parent.Tabs.Add(this);
            }
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            output.SuppressOutput();

            var content = await output.GetChildContentAsync();
            if (!content.IsEmptyOrWhiteSpace)
            {
                output.Content.AppendHtml(content);
            }

            Content = output.Content;
        }

        private IHtmlContent BuildTabPane(TagHelperContent content)
        {
            TagBuilder item = new("div");
            
            item.AddCssClass("tab-pane");
            item.MergeAttribute("role", "tabpanel");

            if (Parent.Fade)
            {
                item.AddCssClass("fade");
            }

            if (Selected)
            {
                if (Parent.Fade)
                {
                    item.AddCssClass("show");
                }

                item.AddCssClass("active");
            }

            item.GenerateId(BuildItemId(), "-");
            //item.MergeAttribute("aria-labelledby", $"{pane.Id}-tab");

            item.InnerHtml.AppendHtml(content.GetContent());

            return item;
        }

        private string BuildItemId()
        {
            return Name.NullEmpty() ?? "{0}-{1}".FormatInvariant(Parent.Id, Index);
        }
    }
}