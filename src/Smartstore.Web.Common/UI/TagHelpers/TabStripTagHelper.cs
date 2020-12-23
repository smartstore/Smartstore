using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Web.UI.TagHelpers
{
    public enum TabsPosition
    {
        Top,
        Right,
        Below,
        Left
    }

    public enum TabsStyle
    {
        Tabs,
        Pills,
        Material
    }

    [OutputElementHint("div")]
    [RestrictChildren("tab")]
    [HtmlTargetElement("tabstrip", Attributes = "id")]
    public class TabStripTagHelper : SmartTagHelper
    {
        [HtmlAttributeNotBound]
        public List<TabTagHelper> Tabs { get; set; } = new();

        /// <summary>
        /// Whether to hide tabstrip nav if there's only one tab item. Default = false.
        /// </summary>
        public bool HideSingleItem { get; set; }

        /// <summary>
        /// Whether to hide tabstrip if there's only one tab item. Default = false.
        /// </summary>
        public bool Responsive { get; set; }

        /// <summary>
        /// When given device expression matches, the tabstrip switches to responsive/compact mode,
        /// but only when <see cref="Responsive"/> is True (e.g.: "&gt;md", "&lt;=lg" etc.).
        /// Default = &lt;=lg
        /// </summary>
        public string Breakpoint { get; set; }

        /// <summary>
        /// Tab nav position
        /// </summary>
        [HtmlAttributeName("nav-position")]
        public TabsPosition Position { get; set; }

        /// <summary>
        /// Tab nav style
        /// </summary>
        [HtmlAttributeName("nav-style")]
        public TabsStyle Style { get; set; }

        /// <summary>
        /// Whether to activate fade animations. Default = true.
        /// </summary>
        public bool Fade { get; set; } = true;

        /// <summary>
        /// Whether to reselect active tab on page reload. Default = true.
        /// </summary>
        public bool SmartTabSelection { get; set; } = true;

        [HtmlAttributeName("onajaxbegin")]
        public string OnAjaxBegin { get; set; }

        [HtmlAttributeName("onajaxsuccess")]
        public string OnAjaxSuccess { get; set; }

        [HtmlAttributeName("onajaxfailure")]
        public string OnAjaxFailure { get; set; }

        [HtmlAttributeName("onajaxcomplete")]
        public string OnAjaxComplete { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(TabStripTagHelper)] = this;
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.GetChildContentAsync();

            if (Tabs.Count == 0)
            {
                output.SuppressOutput();
            }

            MoveSpecialTabToEnd(Tabs);

            var hasContent = Tabs.Any(x => !x.TabInnerContent.IsEmptyOrWhiteSpace || x.Ajax);
            var isTabbable = Position != TabsPosition.Top;
            var isStacked = Position == TabsPosition.Left || Position == TabsPosition.Right;

            output.TagName = "div";
            output.AppendCssClass("tabbable");

            if (isTabbable)
            {
                output.AppendCssClass("tabs-{0}".FormatInvariant(Position.ToString().ToLower()));
            }

            if (SmartTabSelection)
            {
                output.AppendCssClass("tabs-autoselect");
                // TODO: (core) Move SetSelectedTab action to a public shared frontend controller
                output.Attributes.Add("data-tabselector-href", UrlHelper.Action("SetSelectedTab", "Common", new { area = "admin" }));
            }

            if (isStacked)
            {
                output.AppendCssClass("row");
            }

            if (OnAjaxBegin.HasValue())
            {
                output.Attributes.Add("data-ajax-onbegin", OnAjaxBegin);
            }

            if (OnAjaxSuccess.HasValue())
            {
                output.Attributes.Add("data-ajax-onsuccess", OnAjaxSuccess);
            }

            if (OnAjaxFailure.HasValue())
            {
                output.Attributes.Add("data-ajax-onfailure", OnAjaxFailure);
            }

            if (OnAjaxComplete.HasValue())
            {
                output.Attributes.Add("data-ajax-oncomplete", OnAjaxComplete);
            }

            if (Responsive)
            {
                output.AppendCssClass("nav-responsive");
                output.Attributes.Add("data-breakpoint", Breakpoint);
            }

            //// Fix selected tab
            //if (!Tabs.Any(x => x.Selected))
            //{
            //    var selectedTab = Tabs.FirstOrDefault(x => !x.Disabled);
            //    if (selectedTab != null)
            //    {
            //        selectedTab.Selected = true;
            //    }
            //}

            // nav/items
            RenderNav(output.PreContent, isStacked);

            // tab-content
            RenderTabContent(output.Content);
        }

        private void RenderNav(TagHelperContent content, bool isStacked)
        {
            TagBuilder ul = new("ul");
            ul.AppendCssClass("nav");

            if (Style == TabsStyle.Tabs)
            {
                ul.AppendCssClass("nav-tabs");
            }
            else if (Style == TabsStyle.Pills)
            {
                ul.AppendCssClass("nav-pills");
            }
            else if (Style == TabsStyle.Material)
            {
                ul.AppendCssClass("nav-tabs nav-tabs-line");
            }

            if (HideSingleItem && Tabs.Count == 1)
            {
                ul.AppendCssClass("d-none");
            }

            if (isStacked)
            {
                ul.AppendCssClass("flex-row flex-lg-column");
            }

            content.AppendHtml(ul.RenderStartTag());

            foreach (var tab in Tabs)
            {
                content.AppendHtml(tab.TabItemTag);
            }

            content.AppendHtml(ul.RenderEndTag());
        }

        private void RenderTabContent(TagHelperContent content)
        {
            content.AppendHtml("<div class=\"tab-content\">");

            // TODO: tab-content-header
            foreach (var tab in Tabs)
            {
                content.AppendHtml(tab.TabOuterTag);
            }

            content.AppendHtml("</div>");
        }

        private static void MoveSpecialTabToEnd(List<TabTagHelper> tabs)
        {
            var idx = tabs.FindIndex(x => x.Name == "tab-special-plugin-widgets");
            if (idx > -1 && idx < (tabs.Count - 1))
            {
                var tab = tabs[idx];
                tabs.RemoveAt(idx);
                tabs.Add(tab);
            }
        }
    }
}
