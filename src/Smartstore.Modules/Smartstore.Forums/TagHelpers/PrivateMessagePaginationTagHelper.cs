using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Forums.TagHelpers
{
    [OutputElementHint("div")]
    [HtmlTargetElement("pm-pagination", TagStructure = TagStructure.WithoutEndTag)]
    public class PrivateMessagePaginationTagHelper : PaginationTagHelper
    {
        protected override string GenerateUrl(int pageNumber)
        {
            if (QueryParamName.EqualsNoCase("sentPage"))
            {
                return UrlHelper.RouteUrl("PrivateMessages", new { tab = "sent", sentPage = pageNumber });
            }

            return UrlHelper.RouteUrl("PrivateMessages", new { tab = "inbox", inboxPage = pageNumber });
        }
    }
}
