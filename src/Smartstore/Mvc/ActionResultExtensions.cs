using Microsoft.AspNetCore.Mvc;

namespace Smartstore
{
    public static class ActionResultExtensions
    {
        public static bool IsHtmlViewResult(this IActionResult result)
        {
            if (result is null)
            {
                return false;
            }

            if (result is ContentResult contentResult)
            {
                return contentResult.ContentType != null && contentResult.ContentType.StartsWith("text/html");
            }

            if (result is (ViewResult or PartialViewResult or ViewComponentResult))
            {
                return true;
            }

            if (result is IActionResultContainer container)
            {
                return IsHtmlViewResult(container.InnerResult);
            }

            return false;
        }
    }
}
