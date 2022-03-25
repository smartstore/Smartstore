using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Seo;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Applies data from <see cref="IPageAssetBuilder"/> to various HTML elements.
    /// </summary>
    [HtmlTargetElement("html")]
    [HtmlTargetElement("body")]
    [HtmlTargetElement("title")]
    public class PageAssetsTagHelper : TagHelper
    {
        private readonly IPageAssetBuilder _assetBuilder;
        private readonly SeoSettings _seoSettings;

        public PageAssetsTagHelper(IPageAssetBuilder assetBuilder, SeoSettings seoSettings)
        {
            _assetBuilder = assetBuilder;
            _seoSettings = seoSettings;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (output.TagName == "html")
            {
                _assetBuilder.RootAttributes.CopyTo(output.Attributes);
            }
            else if (output.TagName == "body")
            {
                _assetBuilder.BodyAttributes.CopyTo(output.Attributes);
            }
            else if (output.TagName == "title")
            {
                if (_seoSettings.MetaRobotsContent.HasValue())
                {
                    _assetBuilder.AddMetaRobots(content: _seoSettings.MetaRobotsContent);
                }
            }
        }
    }
}