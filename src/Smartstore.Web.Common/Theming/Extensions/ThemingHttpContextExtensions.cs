using Microsoft.AspNetCore.Http;
using Smartstore.Net;

namespace Smartstore.Web.Theming;

public static class ThemingHttpContextExtensions
{
    extension(HttpContext context)
    {
        internal string GetUserThemeChoiceFromCookie()
        {
            if (context == null)
                return null;

            return context.Request.Cookies[CookieNames.UserThemeChoice].NullEmpty();
        }

        internal void SetUserThemeChoiceInCookie(string value)
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
