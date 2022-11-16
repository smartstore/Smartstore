using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("zone", Attributes = NameAttributeName)]
    public class ZoneTagHelper : SmartTagHelper
    {
        const string NameAttributeName = "name";
        const string ModelAttributeName = "model";
        const string ReplaceContentAttributeName = "replace-content";
        const string RemoveIfEmptyAttributeName = "remove-if-empty";

        private readonly IWidgetSelector _widgetSelector;

        public ZoneTagHelper(IWidgetSelector widgetSelector)
        {
            _widgetSelector = widgetSelector;
        }

        [HtmlAttributeName(NameAttributeName)]
        public virtual string ZoneName { get; set; }

        [HtmlAttributeName(ModelAttributeName)]
        public object Model { get; set; }

        /// <summary>
        /// Specifies whether any default zone content should be removed if at least one 
        /// widget is rendered in the zone.
        /// </summary>
        [HtmlAttributeName(ReplaceContentAttributeName)]
        public bool ReplaceContent { get; set; }

        /// <summary>
        /// Whether to remove the root zone tag when it has no content. 
        /// Only applies to HTML tags like div, span, section etc..
        /// <c>zone</c> tags are always removed. Default: false.
        /// </summary>
        [HtmlAttributeName(RemoveIfEmptyAttributeName)]
        public bool RemoveIfEmpty { get; set; }

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

            // First check if any parent sm-suppress-if-empty-zone TagHelper already generated the content...
            var zoneContent = SuppressIfEmptyZoneTagHelper.GetZoneContent(context, ZoneName);
            // ...if not, generate it here.
            zoneContent ??= await _widgetSelector.GetContentAsync(ZoneName, ViewContext, Model ?? ViewContext.ViewData.Model);

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

        public HtmlZoneTagHelper(IWidgetSelector widgetSelector)
            : base(widgetSelector)
        {
        }

        /// <inheritdoc/>
        [HtmlAttributeName(ZoneNameAttributeName)]
        public override string ZoneName
        {
            get => base.ZoneName;
            set => base.ZoneName = value;
        }
    }
}