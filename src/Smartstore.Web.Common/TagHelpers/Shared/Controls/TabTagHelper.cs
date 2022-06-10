using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("a")]
    [HtmlTargetElement("tab", Attributes = TitleAttributeName, ParentTag = "tabstrip")]
    public class TabTagHelper : SmartTagHelper
    {
        const string NameAttributeName = "sm-name";
        const string TitleAttributeName = "sm-title";
        const string SelectedAttributeName = "sm-selected";
        const string DisabledAttributeName = "sm-disabled";
        const string VisibleAttributeName = "sm-visible";
        const string AjaxAttributeName = "sm-ajax";
        const string IconAttributeName = "sm-icon";
        const string IconClassAttributeName = "sm-icon-class";
        const string BadgeTextAttributeName = "sm-badge-text";
        const string BadgeStyleAttributeName = "sm-badge-style";
        const string SummaryAttributeName = "sm-summary";
        const string ImageUrlAttributeName = "sm-image-url";

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
        [HtmlAttributeName(NameAttributeName)]
        public string Name { get; set; }

        /// <summary>
        /// Title/Caption.
        /// </summary>
        [HtmlAttributeName(TitleAttributeName)]
        public string Title { get; set; }

        /// <summary>
        /// Whether item is in selected state.
        /// </summary>
        [HtmlAttributeName(SelectedAttributeName)]
        public bool Selected { get; set; }

        /// <summary>
        /// Whether item is in disabled state.
        /// </summary>
        [HtmlAttributeName(DisabledAttributeName)]
        public bool Disabled { get; set; }

        /// <summary>
        /// Whether item is initially visible. Default = true.
        /// </summary>
        [HtmlAttributeName(VisibleAttributeName)]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Whether to load content deferred per AJAX.
        /// </summary>
        [HtmlAttributeName(AjaxAttributeName)]
        public bool Ajax { get; set; }

        /// <summary>
        /// Icon (class) name (e.g. "fa fa-user", or "bi:user" for Bootstrap icons)
        /// </summary>
        [HtmlAttributeName(IconAttributeName)]
        public string Icon { get; set; }

        /// <summary>
        /// Extra CSS classes for the icon.
        /// </summary>
        [HtmlAttributeName(IconClassAttributeName)]
        public string IconClass { get; set; }

        /// <summary>
        /// Badge text.
        /// </summary>
        [HtmlAttributeName(BadgeTextAttributeName)]
        public string BadgeText { get; set; }

        /// <summary>
        /// Badge style.
        /// </summary>
        [HtmlAttributeName(BadgeStyleAttributeName)]
        public BadgeStyle BadgeStyle { get; set; }

        /// <summary>
        /// Summary to display when tab collapses on smaller devices.
        /// </summary>
        [HtmlAttributeName(SummaryAttributeName)]
        public string Summary { get; set; }

        /// <summary>
        /// Image URL.
        /// </summary>
        [HtmlAttributeName(ImageUrlAttributeName)]
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

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

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