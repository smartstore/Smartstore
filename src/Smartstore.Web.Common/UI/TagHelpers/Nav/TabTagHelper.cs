using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers
{
    [OutputElementHint("a")]
    [HtmlTargetElement("tab", Attributes = "title", ParentTag = "tabstrip")]
    public class TabTagHelper : SmartTagHelper
    {
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

        #region Properties

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
        internal TagHelperContent TabInnerContent { get; set; }

        [HtmlAttributeNotBound]
        internal TagHelperAttributeList Attributes { get; set; }

        [HtmlAttributeNotBound]
        internal bool HasContent
        {
            get => TabInnerContent != null && !TabInnerContent.IsEmptyOrWhiteSpace;
        }

        [HtmlAttributeNotBound]
        internal string TabName
        {
            get => Name ?? Id;
        }

        #endregion

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Process and remember child content (for panes).
            TabInnerContent = await output.GetChildContentAsync();

            // Remember tab item attributes so we can copy them later.
            Attributes = new TagHelperAttributeList(output.Attributes);

            output.TagName = null;
            output.SuppressOutput();
        }

        // Suppress Id auto-generation
        protected override string GenerateTagId(TagHelperContext context) 
            => "{0}-{1}".FormatInvariant(Parent.Id, Index);
    }
}