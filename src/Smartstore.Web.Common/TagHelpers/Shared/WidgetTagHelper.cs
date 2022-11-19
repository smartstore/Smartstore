using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("widget", Attributes = TargetZoneAttributeName)]
    public class WidgetTagHelper : SmartTagHelper
    {
        const string TargetZoneAttributeName = "target-zone";
        const string OrderAttributeName = "order";
        const string PrependAttributeName = "prepend";
        const string KeyAttributeName = "key";

        private readonly IWidgetProvider _widgetProvider;

        public WidgetTagHelper(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        public override int Order => int.MaxValue;

        /// <summary>
        /// The target zone name to inject this widget to.
        /// </summary>
        [HtmlAttributeName(TargetZoneAttributeName)]
        public virtual string TargetZone { get; set; }

        /// <summary>
        /// The order within the target zone.
        /// </summary>
        [HtmlAttributeName(OrderAttributeName)]
        public virtual int Ordinal { get; set; }

        /// <summary>
        /// Whether the widget output should be inserted BEFORE target zone's existing content. 
        /// Omitting this attribute renders widget output AFTER any existing content.
        /// </summary>
        [HtmlAttributeName(PrependAttributeName)]
        public virtual bool Prepend { get; set; }

        /// <summary>
        /// When set, ensures uniqueness within a particular zone.
        /// </summary>
        [HtmlAttributeName(KeyAttributeName)]
        public virtual string Key { get; set; }

        protected override string GenerateTagId(TagHelperContext context)
            => null;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (TargetZone.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }

            if (Key.HasValue() && _widgetProvider.ContainsWidget(TargetZone, Key))
            {
                output.SuppressOutput();
                return;
            }

            if (ViewContext.HttpContext.Request.IsAjax())
            {
                //// Don't re-inject content during AJAX requests, the target zones are most probably rendered already.
                //// Just output the content in-place.
                //if (output.TagName == "widget")
                //{
                //    output.TagName = null;
                //}
                //else if (output.TagName == "meta")
                //{
                //    output.SuppressOutput();
                //}

                output.SuppressOutput();

                return;
            }

            TagHelperContent childContent = await output.GetChildContentAsync();
            TagHelperContent content;

            if (output.TagName == "widget")
            {
                if (childContent.IsEmptyOrWhiteSpace)
                {
                    output.SuppressOutput();
                    return;
                }

                // Never render <widget> tag, only the content
                output.TagName = null;
                content = childContent;
            }
            else
            {
                output.Content.SetHtmlContent(childContent);
                content = new DefaultTagHelperContent();
                output.CopyTo(content);
            }

            output.SuppressOutput();

            if (!content.IsEmptyOrWhiteSpace)
            {
                var widget = new HtmlWidget(content) { Order = Ordinal, Prepend = Prepend, Key = Key };
                _widgetProvider.RegisterWidget(TargetZone, widget);
            }
        }
    }

    [HtmlTargetElement("script", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("style", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("link", Attributes = TargetZoneAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("meta", Attributes = TargetZoneAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("div", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("span", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("section", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("form", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("ul", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("ol", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("svg", Attributes = TargetZoneAttributeName)]
    [HtmlTargetElement("img", Attributes = TargetZoneAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("a", Attributes = TargetZoneAttributeName)]
    public class HtmlWidgetTagHelper : WidgetTagHelper
    {
        const string TargetZoneAttributeName = "sm-target-zone";
        const string OrderAttributeName = "sm-order";
        const string PrependAttributeName = "sm-prepend";
        const string KeyAttributeName = "sm-key";

        public HtmlWidgetTagHelper(IWidgetProvider widgetProvider)
            : base(widgetProvider)
        {
        }

        /// <inheritdoc/>
        [HtmlAttributeName(TargetZoneAttributeName)]
        public override string TargetZone
        {
            get => base.TargetZone;
            set => base.TargetZone = value;
        }

        /// <inheritdoc/>
        [HtmlAttributeName(OrderAttributeName)]
        public override int Ordinal
        {
            get => base.Ordinal;
            set => base.Ordinal = value;
        }

        /// <inheritdoc/>
        [HtmlAttributeName(PrependAttributeName)]
        public override bool Prepend
        {
            get => base.Prepend;
            set => base.Prepend = value;
        }

        /// <inheritdoc/>
        [HtmlAttributeName(KeyAttributeName)]
        public override string Key
        {
            get => base.Key;
            set => base.Key = value;
        }
    }
}
