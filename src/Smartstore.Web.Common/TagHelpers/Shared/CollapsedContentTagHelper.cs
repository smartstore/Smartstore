using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Catalog;
using Smartstore.Utilities;

namespace Smartstore.Web.TagHelpers.Shared;

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
        if (context.ShouldSuppressChildContent())
        {
            return;
        }

        output.TagName = null;
        await output.LoadAndSetChildContentAsync();

        if (_catalogSettings.EnableHtmlTextCollapser && (MaxHeight == null || MaxHeight > 0))
        {
            var maxHeight = MaxHeight ?? _catalogSettings.HtmlTextCollapsedHeight;
            var classes = output.Attributes.TryGetAttribute("class", out var attr) ? $"text-expander {attr.Value}" : "text-expander";
            var id = "text-expander" + CommonHelper.GenerateRandomInteger();

            var outer = new TagBuilder("div");
            outer.MergeAttribute("id", id);
            outer.Attributes.Add("class", classes);
            outer.Attributes.Add("data-max-height", maxHeight.ToString());

            var inner = new TagBuilder("div");
            inner.Attributes.Add("id", id + "-content");
            inner.Attributes.Add("class", "text-expander-content");

            output.WrapContentWith(outer, inner);
        }
    }
}
