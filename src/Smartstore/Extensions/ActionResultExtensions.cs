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
                return contentResult.ContentType.EqualsNoCase("text/html");
            }

            return result is (ViewResult or PartialViewResult or ViewComponentResult);
        }
    }
}
