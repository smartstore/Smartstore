using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.Collections;
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

            cookies.Delete(CookieNames.UserThemeChoice);

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
