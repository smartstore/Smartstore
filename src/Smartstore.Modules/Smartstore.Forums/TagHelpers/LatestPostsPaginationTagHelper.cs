using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Forums.TagHelpers
{
    [OutputElementHint("div")]
    [HtmlTargetElement("latestposts-pagination", TagStructure = TagStructure.WithoutEndTag)]
    public class LatestPostsPaginationTagHelper : PaginationTagHelper
    {
        const string CustomerIdAttributeName = "sm-customer-id";

        [HtmlAttributeName(CustomerIdAttributeName)]
        public int CustomerId { get; set; }

        protected override string GenerateUrl(int pageNumber)
        {
            return UrlHelper.RouteUrl("CustomerProfile", new { id = CustomerId, latestPostPage = pageNumber });
        }
    }
}
