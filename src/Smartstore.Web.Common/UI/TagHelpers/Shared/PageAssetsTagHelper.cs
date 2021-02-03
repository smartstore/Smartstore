using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.UI.TagHelpers.Shared
{
    /// <summary>
    /// Applies data from <see cref="IPageAssetBuilder"/> to various HTML elements.
    /// </summary>
    [HtmlTargetElement("html")]
    [HtmlTargetElement("body")]
    //[HtmlTargetElement("script", Attributes = TagLocationAttributeName)]
    //[HtmlTargetElement("link", Attributes = TagLocationAttributeName)]
    public class PageAssetsTagHelper : TagHelper
    {
        const string TagLocationAttributeName = "asp-tag-location";

        private readonly IPageAssetBuilder _assetBuilder;

        public PageAssetsTagHelper(IPageAssetBuilder assetBuilder)
        {
            _assetBuilder = assetBuilder;
        }

        public AssetLocation? TagLocation { get; set; }

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
            else if (TagLocation.HasValue)
            {
                // Is script or link
            }
        }
    }
}
