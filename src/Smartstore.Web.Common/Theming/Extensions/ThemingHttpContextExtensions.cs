using Microsoft.AspNetCore.Http;
using Smartstore.Net;

namespace Smartstore.Web.Theming
{
    public static class ThemingHttpContextExtensions
    {
        internal static string GetUserThemeChoiceFromCookie(this HttpContext context)
        {
            if (context == null)
                return null;

            return context.Request.Cookies[CookieNames.UserThemeChoice].NullEmpty();
        }

        internal static void SetUserThemeChoiceInCookie(this HttpContext context, string value)
        {
            if (context == null)
            {
                return;
            }

            var cookies = context.Response.Cookies;
            var cookieName = CookieNames.UserThemeChoice;

            cookies.Delete(cookieName);

            if (value.IsEmpty())
            {
                return;
            }

            cookies.Append(cookieName, value, new CookieOptions
            {
                Expires = DateTime.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true
            });
        }
    }
}
