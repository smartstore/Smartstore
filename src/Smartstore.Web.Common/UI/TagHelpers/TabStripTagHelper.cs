using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
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
            var classList = output.GetClassList();

            output.TagName = "div";
            classList.Add("tabbable");

            if (isTabbable)
            {
                classList.Add("tabs-{0}".FormatInvariant(Position.ToString().ToLower()));
            }

            if (SmartTabSelection)
            {
                classList.Add("tabs-autoselect");
                // TODO: (core) Move SetSelectedTab action to a public shared frontend controller
                output.Attributes.Add("data-tabselector-href", UrlHelper.Action("SetSelectedTab", "Common", new { area = "admin" }));
            }

            if (isStacked)
            {
                classList.Add("row");
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
                classList.Add("nav-responsive");
                output.Attributes.Add("data-breakpoint", Breakpoint);
            }

            // Flush classes
            classList.Dispose();

            // tab-content above nav
            if (Position == TabsPosition.Below && hasContent)
            {
                RenderTabContent(output.Content, isStacked);
            }

            // Enable smart tab selection
            string selector = null;
            if (SmartTabSelection)
            {
                selector = TrySelectRememberedTab();
            }

            // nav/items
            RenderNav(output.Content, isStacked);

            // tab-content below nav
            if (Position != TabsPosition.Below && hasContent)
            {
                RenderTabContent(output.Content, isStacked);
            }

            if (selector != null)
            {
                output.Content.AppendHtmlLine(
@"<script>
	$(function() {{
		_.delay(function() {{
			$(""{0}"").trigger(""show"");
		}}, 100);
	}})
</script>".FormatInvariant(selector));
            }

            var loadedTabs = Tabs.Where(x => x.LoadedTabName.HasValue()).ToList();
            if (loadedTabs.Count > 0)
            {
                foreach (var tabName in loadedTabs)
                {
                    output.Content.AppendHtmlLine($"<input type='hidden' class='loaded-tab-name' name='LoadedTabs' value='{tabName}' />");
                }
            }

            if (Responsive /* && tab.TabContentHeaderContent != null*/)
            {
                output.Content.AppendHtmlLine(@"<script>$(function() {{ $('#{0}').responsiveNav(); }})</script>".FormatInvariant(Id));
            }
        }

        private void RenderNav(TagHelperContent content, bool isStacked)
        {
            TagBuilder ul = new("ul");
            var classList = ul.GetClassList();
            classList.Add("nav");

            if (Style == TabsStyle.Tabs)
            {
                classList.Add("nav-tabs");
            }
            else if (Style == TabsStyle.Pills)
            {
                classList.Add("nav-pills");
            }
            else if (Style == TabsStyle.Material)
            {
                classList.Add("nav-tabs", "nav-tabs-line");
            }

            if (HideSingleItem && Tabs.Count == 1)
            {
                classList.Add("d-none");
            }

            if (isStacked)
            {
                classList.Add("flex-row",  "flex-lg-column");
            }

            classList.Dispose();

            if (isStacked)
            {
                // opening left/right tabs col
                content.AppendHtml("<aside class=\"col-lg-auto nav-aside\">");
            }

            content.AppendHtml(ul.RenderStartTag());

            foreach (var tab in Tabs)
            {
                content.AppendHtml(tab.TabItemTag);
            }

            content.AppendHtml(ul.RenderEndTag());

            if (isStacked)
            {
                // closing left/right tabs col
                content.AppendHtml("</aside>");
            }
        }

        private void RenderTabContent(TagHelperContent content, bool isStacked)
        {
            if (isStacked)
            {
                // opening left/right content col
                content.AppendHtml("<div class=\"col-lg nav-content\">");
            }

            content.AppendHtml("<div class=\"tab-content\">");

            // TODO: tab-content-header
            foreach (var tab in Tabs)
            {
                content.AppendHtml(tab.TabOuterTag);
            }

            content.AppendHtml("</div>");

            if (isStacked)
            {
                // closing left/right content col
                content.AppendHtml("</div>");
            }
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

        // 
        /// <summary>
        /// Returns a query selector
        /// </summary>
        private string TrySelectRememberedTab()
        {
            //// TODO: (core) Implement TabStripTagHelper.TrySelectRememberedTab()
            ///
            //var tab = this.Component;

            //if (tab.Id.IsEmpty())
            //    return null;

            //if (ViewContext.ViewData.Model is EntityModelBase model && model.Id == 0)
            //{
            //    // it's a "create" operation: don't select
            //    return null;
            //}

            //var rememberedTab = (SelectedTabInfo)ViewContext.TempData["SelectedTab." + tab.Id];
            //if (rememberedTab != null && rememberedTab.Path.Equals(ViewContext.HttpContext.Request.RawUrl, StringComparison.OrdinalIgnoreCase))
            //{
            //    // get tab to select
            //    var tabToSelect = GetTabById(rememberedTab.TabId);

            //    if (tabToSelect != null)
            //    {
            //        // unselect former selected tab(s)
            //        tab.Items.Each(x => x.Selected = false);

            //        // select the new tab
            //        tabToSelect.Selected = true;

            //        // persist again for the next request
            //        ViewContext.TempData["SelectedTab." + tab.Id] = rememberedTab;

            //        if (tabToSelect.Ajax && tabToSelect.Content == null)
            //        {
            //            return ".nav a[data-ajax-url][href='#{0}']".FormatInvariant(rememberedTab.TabId);
            //        }
            //    }
            //}

            return null;
        }
    }
}
