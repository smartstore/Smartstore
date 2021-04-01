using System;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Smartstore.Net;

namespace Smartstore
{
    public static class HttpWebRequestExtensions
    {
        const string NullIPv6 = "::1";

        /// <summary>
        /// Checks whether the current request originates from a local computer.
        /// </summary>
        public static bool IsLocal(this ConnectionInfo connection)
        {
            Guard.NotNull(connection, nameof(connection));

            var remoteAddress = connection.RemoteIpAddress;
            if (remoteAddress == null || remoteAddress.ToString() == NullIPv6)
            {
                return true;
            }

            // We have a remote address set up.
            // Is local the same as remote, then we are local.
            var localAddress = connection.LocalIpAddress;
            if (localAddress != null && localAddress.ToString() != NullIPv6)
            {
                return remoteAddress.Equals(localAddress);
            }

            // Else we are remote if the remote IP address is not a loopback address
            return IPAddress.IsLoopback(remoteAddress);
        }

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