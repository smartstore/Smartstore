using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

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
    [HtmlTargetElement("tabstrip")]
    public class TabStripTagHelper : SmartTagHelper
    {
        [HtmlAttributeNotBound]
        public List<TabTagHelper> Tabs { get; set; } = new();

        /// <summary>
        /// Whether to hide tabstrip if there's only one tab item. Default = false.
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
            var tabs = Tabs;
            await output.GetChildContentAsync();
            tabs = Tabs;
        }
    }
}
