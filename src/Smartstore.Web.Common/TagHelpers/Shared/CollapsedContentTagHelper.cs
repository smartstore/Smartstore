using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Catalog;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("collapsed-content")]
    public class CollapsedContentTagHelper : TagHelper
    {
        const string MaxHeightAttributeName = "sm-max-height";

        private readonly CatalogSettings _catalogSettings;

        public CollapsedContentTagHelper(CatalogSettings catalogSettings)
        {
            _catalogSettings = catalogSettings;
        }

        [HtmlAttributeName(MaxHeightAttributeName)]
        public int? MaxHeight { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            await output.LoadAndSetChildContentAsync();

            if (_catalogSettings.EnableHtmlTextCollapser)
            {
                var maxHeight = MaxHeight ?? _catalogSettings.HtmlTextCollapsedHeight;

                var outer = new TagBuilder("div");
                outer.Attributes.Add("class", "more-less");
                outer.Attributes.Add("data-max-height", maxHeight.ToString());

                var inner = new TagBuilder("div");
                inner.Attributes.Add("class", "more-block");

                output.WrapContentWith(outer, inner);
            }
        }
    }
}
