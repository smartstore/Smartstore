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

        /// <summary>
        /// Icon class name (e.g. "fa fa-user")
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Badge text.
        /// </summary>
        public string BadgeText { get; set; }

        /// <summary>
        /// Badge style.
        /// </summary>
        public BadgeStyle BadgeStyle { get; set; }

        /// <summary>
        /// Summary to display when tab collapses on smaller devices.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Image URL.
        /// </summary>
        public string ImageUrl { get; set; }

        [HtmlAttributeNotBound]
        internal TabStripTagHelper Parent { get; set; }

        [HtmlAttributeNotBound]
        internal int Index { get; set; }

        [HtmlAttributeNotBound]
        internal string LoadedTabName { get; set; }

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
            TabInnerContent = await output.GetChildContentAsync();
            TabOuterTag = BuildTabPane(TabInnerContent);
            TabItemTag = BuildTabItem(context, output);

            output.TagName = null;
            output.SuppressOutput();
        }

        private TagBuilder BuildTabPane(IHtmlContent content)
        {
            TagBuilder div = new("div");

            var classList = div.GetClassList();
            classList.Add("tab-pane");

            div.MergeAttribute("role", "tabpanel");

            if (Parent.Fade)
            {
                classList.Add("fade");

                if (Selected)
                {
                    classList.Add("show");
                }
            }

            if (Selected)
            {
                classList.Add("active");
            }

            div.GenerateId(Id, "-");
            div.MergeAttribute("aria-labelledby", $"{div.Attributes["id"]}-tab");

            classList.Dispose();
            div.InnerHtml.SetHtmlContent(content);

            return div;
        }

        private TagBuilder BuildTabItem(TagHelperContext context, TagHelperOutput output)
        {
            string temp = string.Empty;

            // <li [class="nav-item [d-none]"]><a href="#{id}" class="nav-link [active]" data-toggle="tab">{text}</a></li>
            TagBuilder li = new("li");
            
            // Copy all attributes from output to div tag (except for "id")
            foreach (var attr in output.Attributes)
            {
                li.MergeAttribute(attr.Name, attr.Value?.ToString());
            }
            li.Attributes.TryRemove("id", out _);
            li.Attributes.TryRemove("href", out _);

            li.AppendCssClass("nav-item");

            if (!Selected && !Visible)
            {
                li.AppendCssClass("d-none");
            }

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
                    LoadedTabName = GetTabName(output) ?? itemId;
                }
                else
                {
                    // No content, create real link instead
                    var url = output.Attributes["href"]?.Value?.ToString();

                    if (url == null)
                    {
                        a.MergeAttribute("href", "#");
                    }
                    else
                    {
                        if (Ajax)
                        {
                            a.MergeAttribute("href", itemId);
                            a.MergeAttribute("data-ajax-url", url);
                            a.MergeAttribute("data-toggle", "tab");
                        }
                        else
                        {
                            a.MergeAttribute("href", UrlHelper.Content(url));
                        }
                    }
                }

                if (BadgeText.HasValue())
                {
                    a.AppendCssClass("clearfix");
                }

                // Icon/Image
                BuildTabIcon(a);

                // Caption
                BuildTabCaption(a);

                // Badge
                BuildTabBadge(a);

                // Nav link short summary for collapsed state
                BuildTabSummary(a);

                li.InnerHtml.SetHtmlContent(a);
            }

            return li;
        }

        private void BuildTabIcon(TagBuilder a)
        {
            if (Icon.HasValue())
            {
                TagBuilder i = new("i");
                i.AddCssClass(Icon);
                a.InnerHtml.AppendHtml(i);
            }
            else if (ImageUrl.HasValue())
            {
                TagBuilder img = new("img");
                img.Attributes["src"] = UrlHelper.Content(ImageUrl);
                img.Attributes["alt"] = "Icon";
                a.InnerHtml.AppendHtml(img);
            }
        }

        private void BuildTabCaption(TagBuilder a)
        {
            TagBuilder caption = new("span");
            caption.AppendCssClass("tab-caption");
            caption.InnerHtml.Append(Title);
            a.InnerHtml.AppendHtml(caption);
        }

        private void BuildTabBadge(TagBuilder a)
        {
            if (BadgeText.HasValue())
            {
                var temp = "ml-2 badge";
                temp += " badge-" + BadgeStyle.ToString().ToLower();
                if (Parent.Position == TabsPosition.Left)
                {
                    temp += " float-right"; // looks nicer 
                }

                TagBuilder span = new("span");
                span.AddCssClass(temp);
                span.InnerHtml.Append(BadgeText);
                a.InnerHtml.AppendHtml(span);
            }
        }

        private void BuildTabSummary(TagBuilder a)
        {
            if (Parent.Responsive && Summary.HasValue())
            {
                TagBuilder span = new("span");
                span.AddCssClass("nav-link-summary");
                span.InnerHtml.Append(Summary);
                a.InnerHtml.AppendHtml(span);
            }
        }

        private static string GetTabName(TagHelperOutput output)
        {
            if (output.Attributes.TryGetAttribute("data-tab-name", out var attr))
            {
                return attr.Value?.ToString();
            }

            return null;
        }

        // Suppress Id auto-generation
        protected override string GenerateTagId(TagHelperContext context) 
            => "{0}-{1}".FormatInvariant(Parent.Id, Index);
    }
}