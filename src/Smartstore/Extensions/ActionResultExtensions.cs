using Microsoft.AspNetCore.Mvc;

namespace Smartstore
{
    public static class ActionResultExtensions
    {
        public static bool IsHtmlViewResult(this IActionResult result)
        {
            var contentResult = result as ContentResult;
            if (contentResult != null)
            {
                return contentResult.ContentType.EqualsNoCase("text/html");
            }

            return false;
        }
    }
}
