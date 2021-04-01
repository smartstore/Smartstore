using System;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Smartstore.Net;

namespace Smartstore
{
    public static class HttpWebRequestExtensions
    {        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAuthenticationCookie(this HttpWebRequest webRequest, HttpRequest httpRequest)
        {
            CopyCookie(webRequest, httpRequest, CookieNames.Identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetVisitorCookie(this HttpWebRequest webRequest, HttpRequest httpRequest)
        {
            CopyCookie(webRequest, httpRequest, CookieNames.Visitor);
        }

        private static void CopyCookie(HttpWebRequest webRequest, HttpRequest sourceHttpRequest, string cookieName)
        {
            Guard.NotNull(webRequest, nameof(webRequest));
            Guard.NotNull(sourceHttpRequest, nameof(sourceHttpRequest));
            Guard.NotEmpty(cookieName, nameof(cookieName));

            var sourceCookie = sourceHttpRequest.Cookies[cookieName];
            if (sourceCookie == null)
                return;

            // TODO: (core) CopyCookie > How to obtain cookie Path (?)
            var sendCookie = new Cookie(cookieName, sourceCookie, null, sourceHttpRequest.Host.Value);

            if (webRequest.CookieContainer == null)
            {
                webRequest.CookieContainer = new CookieContainer();
            }

            webRequest.CookieContainer.Add(sendCookie);
        }
    }
}