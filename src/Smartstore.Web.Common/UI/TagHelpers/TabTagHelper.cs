using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    [OutputElementHint("a")]
    [HtmlTargetElement("tab", Attributes = "title", ParentTag = "tabstrip")]
    public class TabTagHelper : SmartTagHelper
    {
        /// <summary>
        /// Unique name of tab item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Title/Caption.
        /// </summary>
        public string Title { get; set; }

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
        internal TagHelperContent TabInnerContent { get; set; }

        [HtmlAttributeNotBound]
        internal TagBuilder TabOuterTag { get; set; }

        [HtmlAttributeNotBound]
        internal TagBuilder TabItemTag { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);

            if (Selected && Disabled)
            {
                Disabled = false;
            }

            if (Selected && !Visible)
            {
                Visible = true;
            }

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

            TabInnerContent = await output.GetChildContentAsync();
            TabOuterTag = BuildTabPane(TabInnerContent);
            TabItemTag = BuildTabItem();

            output.SuppressOutput();
        }

        private TagBuilder BuildTabItem()
        {
            string temp = string.Empty;
            string loadedTabName = null;

            TagBuilder li = new("li");

            li.AppendCssClass("nav-item");

            {
                TagBuilder a = new("a");

                // Link/Target
                var itemId = "#" + Id;
                a.AppendCssClass("nav-link" + (Selected ? " active" : ""));

                if (!TabInnerContent.IsEmptyOrWhiteSpace)
                {
                    a.MergeAttribute("href", itemId);
                    a.MergeAttribute("data-toggle", "tab");
                    a.MergeAttribute("data-loaded", "true");
                    //loadedTabName = GetTabName(item) ?? itemId; // ...
                }
                else
                {
                    // No content, create real link instead
                    // ...
                }

                // Icon/Image
                // // ...

                // Caption
                TagBuilder caption = new("span");
                caption.AppendCssClass("tab-caption");
                caption.InnerHtml.Append(Title);
                a.InnerHtml.AppendHtml(caption);

                // Badge
                // ...

                // Nav link short summary for collapsed state
                // ...

                li.InnerHtml.SetHtmlContent(a);
            }

            return li;
        }

        private TagBuilder BuildTabPane(IHtmlContent content)
        {
            TagBuilder div = new("div");
            
            div.AppendCssClass("tab-pane");
            div.MergeAttribute("role", "tabpanel");

            if (Parent.Fade)
            {
                div.AppendCssClass("fade");

                if (Selected)
                {
                    div.AppendCssClass("show");
                }
            }

            if (Selected)
            {
                div.AppendCssClass("active");
            }

            div.GenerateId(Id, "-");
            div.MergeAttribute("aria-labelledby", $"{div.Attributes["id"]}-tab");

            div.InnerHtml.SetHtmlContent(content);

            return div;
        }

        // Suppress Id auto-generation
        protected override string GenerateTagId(TagHelperContext context) 
            => "{0}-{1}".FormatInvariant(Parent.Id, Index);
    }
}