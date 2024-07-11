using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Events;
using Smartstore.Web.Rendering.Events;

namespace Smartstore.Web.TagHelpers.Shared
{
    [DebuggerDisplay("Zone: {Name}")]
    [HtmlTargetElement("zone", Attributes = NameAttributeName)]
    public class ZoneTagHelper : SmartTagHelper, IWidgetZone
    {
        const string NameAttributeName = "name";
        const string ModelAttributeName = "model";
        const string ReplaceContentAttributeName = "replace-content";
        const string RemoveIfEmptyAttributeName = "remove-if-empty";
        const string PreviewDisabledAttributeName = "preview-disabled";
        const string PreviewCssClassAttributeName = "preview-class";
        const string PreviewCssStyleAttributeName = "preview-style";
        const string PreviewTagAttributeName = "preview-tag";

        private readonly IWidgetSelector _widgetSelector;
        private readonly IEventPublisher _eventPublisher;

        public ZoneTagHelper(IWidgetSelector widgetSelector, IEventPublisher eventPublisher)
        {
            _widgetSelector = widgetSelector;
            _eventPublisher = eventPublisher;
        }

        [HtmlAttributeName(NameAttributeName)]
        public virtual string Name { get; set; }

        [HtmlAttributeName(ModelAttributeName)]
        public object Model { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(ReplaceContentAttributeName)]
        public bool ReplaceContent { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(RemoveIfEmptyAttributeName)]
        public bool RemoveIfEmpty { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(PreviewDisabledAttributeName)]
        public bool PreviewDisabled { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(PreviewCssClassAttributeName)]
        public string PreviewCssClass { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(PreviewCssStyleAttributeName)]
        public string PreviewCssStyle { get; set; }

        /// <inheritdoc />
        [HtmlAttributeName(PreviewTagAttributeName)]
        public string PreviewTagName { get; set; }

        protected override string GenerateTagId(TagHelperContext context) 
            => null;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var isHtmlTag = output.TagName != "zone";
            if (!isHtmlTag)
            {
                // Never render <zone> tag
                output.TagName = null;
            }

            // Obtain view model.
            var model = Model ?? ViewContext.ViewData.Model;

            // Generate zone content by iterating all widgets and invoking them.
            var zoneContent = await _widgetSelector.GetContentAsync(this, ViewContext, model);

            // Publish event to give integrators a chance to inject custom content to the zone.
            var renderEvent = new ViewZoneRenderingEvent(this, zoneContent, ViewContext)
            {
                Model = model
            };
            await _eventPublisher.PublishAsync(renderEvent);

            if (zoneContent.IsEmptyOrWhiteSpace)
            {
                // No widgets
                if (RemoveIfEmpty && output.TagName.HasValue())
                {
                    var childContent = await output.GetChildContentAsync();
                    if (childContent.IsEmptyOrWhiteSpace)
                    {
                        output.TagName = null;
                    }
                }
            }
            else
            {
                if (ReplaceContent)
                {
                    output.Content.SetContent(string.Empty);
                }

                if (zoneContent.HasPreContent)
                {
                    output.PreContent.AppendHtml(zoneContent.PreContent);
                }

                if (zoneContent.HasPostContent)
                {
                    output.PostContent.AppendHtml(zoneContent.PostContent);
                }
            }
        }
    }

    [HtmlTargetElement("div", Attributes = ZoneNameAttributeName)]
    [HtmlTargetElement("span", Attributes = ZoneNameAttributeName)]
    [HtmlTargetElement("p", Attributes = ZoneNameAttributeName)]
    [HtmlTargetElement("section", Attributes = ZoneNameAttributeName)]
    [HtmlTargetElement("aside", Attributes = ZoneNameAttributeName)]
    [HtmlTargetElement("header", Attributes = ZoneNameAttributeName)]
    [HtmlTargetElement("footer", Attributes = ZoneNameAttributeName)]
    public class HtmlZoneTagHelper : ZoneTagHelper
    {
        const string ZoneNameAttributeName = "zone-name";

        public HtmlZoneTagHelper(IWidgetSelector widgetSelector, IEventPublisher eventPublisher)
            : base(widgetSelector, eventPublisher)
        {
        }

        /// <inheritdoc/>
        [HtmlAttributeName(ZoneNameAttributeName)]
        public override string Name
        {
            get => base.Name;
            set => base.Name = value;
        }
    }
}