using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.Collections;
using Smartstore.IO;
using Smartstore.Net;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class HttpExtensions
    {
        const string NullIPv6 = "::1";

        private static readonly List<(string, string)> _sslHeaders = new List<(string, string)>
        {
            ("HTTP_CLUSTER_HTTPS", "on"),
            ("HTTP_X_FORWARDED_PROTO", "https"),
            ("X-Forwarded-Proto", "https"),
            ("x-arr-ssl", null),
            ("X-Forwarded-Protocol", "https"),
            ("X-Forwarded-Ssl", "on"),
            ("X-Url-Scheme", "https")
        };

        public static ILifetimeScope GetServiceScope(this HttpContext httpContext)
        {
            return httpContext.RequestServices.AsLifetimeScope();
        }

        /// <summary>
        /// Gets a typed route value from <see cref="Microsoft.AspNetCore.Routing.RouteData.Values"/> associated
        /// with the provided <paramref name="httpContext"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the route value to.</typeparam>
        /// <param name="key">The key of the route value.</param>
        /// <param name="defaultValue">The default value to return if route parameter does not exist.</param>
        /// <returns>The corresponding typed route value, or passed <paramref name="defaultValue"/>.</returns>
        public static T GetRouteValueAs<T>(this HttpContext httpContext, string key, T defaultValue = default)
        {
            if (httpContext.TryGetRouteValueAs(key, out T value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Tries to read a route value from <see cref="Microsoft.AspNetCore.Routing.RouteData.Values"/> associated
        /// with the provided <paramref name="httpContext"/>, and converts it to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert the route value to.</typeparam>
        /// <param name="key">The key of the route value.</param>
        /// <param name="value">The found and converted value.</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present and could be converted, <c>false</c> otherwise.</returns>
        public static bool TryGetRouteValueAs<T>(this HttpContext httpContext, string key, out T value)
        {
            value = default;

            var routeValue = httpContext.GetRouteValue(key);
            if (routeValue != null)
            {
                return CommonHelper.TryConvert(routeValue, out value);
            }

            return false;
        }

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="values">The found values if any</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present, <c>false</c> otherwise.</returns>
        public static bool TryGetValue(this HttpRequest request, string key, out StringValues values)
        {
            values = StringValues.Empty;

            if (request.HasFormContentType)
            {
                values = request.Form[key];
            }

            if (values == StringValues.Empty)
            {
                values = request.Query[key];
            }

            return values != StringValues.Empty;
        }

        /// <summary>
        /// Tries to read a request value first from <see cref="HttpRequest.Form"/> (if method is POST), then from
        /// <see cref="HttpRequest.Query"/>, and converts value to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">The found and converted value</param>
        /// <returns><c>true</c> if a value with passed <paramref name="key"/> was present and could be converted, <c>false</c> otherwise.</returns>
        public static bool TryGetValueAs<T>(this HttpRequest request, string key, out T value)
        {
            value = default;

            if (request.TryGetValue(key, out var values))
            {
                return CommonHelper.TryConvert(values.ToString(), out value);
            }

            return false;
        }

        public static string UserAgent(this HttpRequest httpRequest)
        {
            if (httpRequest.Headers.TryGetValue(HeaderNames.UserAgent, out var value))
            {
                return value.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the raw request path (PathBase + Path + QueryString)
        /// </summary>
        /// <returns>The raw URL</returns>
        public static string RawUrl(this HttpRequest httpRequest)
        {
            // Try to resolve unencoded raw value from feature.
            var url = httpRequest.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;

            if (url.IsEmpty())
            {
                // Fallback
                url = httpRequest.PathBase + httpRequest.Path + httpRequest.QueryString;
            }

            return url;
        }

        /// <summary>
        /// Gets a value which indicates whether the HTTP connection uses secure sockets (HTTPS protocol). 
        /// Works with cloud's load balancers.
        /// </summary>
        public static bool IsSecureConnection(this HttpRequest httpRequest)
        {
            if (httpRequest.IsHttps)
            {
                return true;
            }

            foreach (var tuple in _sslHeaders)
            {
                var serverVar = httpRequest.Headers[tuple.Item1];
                if (serverVar != StringValues.Empty)
                {
                    return tuple.Item2 == null || tuple.Item2.Equals(serverVar, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the current request is an AJAX request.
        /// </summary>
        /// <param name="httpRequest"></param>
        public static bool IsAjaxRequest(this HttpRequest httpRequest)
        {
            return 
                string.Equals(httpRequest.Headers[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal) ||
                string.Equals(httpRequest.Query[HeaderNames.XRequestedWith], "XMLHttpRequest", StringComparison.Ordinal);
        }

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

        /// <summary>
        /// Gets a value which indicates whether the current request requests a static resource, like .txt, .pdf, .js, .css etc.
        /// </summary>
        public static bool IsStaticResourceRequested(this HttpRequest request)
        {
            if (request is null)
                return false;

            return MimeTypes.TryMapNameToMimeType(request.Path, out _);
        }

        public static T GetItem<T>(this HttpContext httpContext, string key, Func<T> factory = null, bool forceCreation = true)
        {
            Guard.NotEmpty(key, nameof(key));

            var items = httpContext?.Items;
            if (items == null)
            {
                return default;
            }

            if (items.ContainsKey(key))
            {
                return (T)items[key];
            }
            else
            {
                if (forceCreation)
                {
                    var item = items[key] = (factory ?? (() => default)).Invoke();
                    return (T)item;
                }
                else
                {
                    return default;
                }
            }
        }

        public static async Task<T> GetItemAsync<T>(this HttpContext httpContext, string key, Func<Task<T>> factory, bool forceCreation = true)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(factory, nameof(factory));

            var items = httpContext?.Items;
            if (items == null)
            {
                return default;
            }

            if (items.ContainsKey(key))
            {
                return (T)items[key];
            }
            else
            {
                if (forceCreation)
                {
                    var item = items[key] = await factory();
                    return (T)item;
                }
                else
                {
                    return default;
                }
            }
        }

        public static MutableQueryCollection GetPreviewModeFromCookie(this HttpContext context)
        {
            var request = context?.Request;

            if (request != null)
            {
                var cookieValue = request.Cookies[CookieNames.PreviewModeOverride].NullEmpty();
                if (cookieValue != null)
                {
                    return new MutableQueryCollection('?' + cookieValue);
                }
            }

            return new MutableQueryCollection();
        }

        public static void SetPreviewModeValueInCookie(this HttpContext context, string name, string value)
        {
            Guard.NotEmpty(name, nameof(name));

            if (context == null)
            {
                return;
            }

            var cookies = context.Response.Cookies;
            var cookieName = CookieNames.PreviewModeOverride;
            var cookie = GetPreviewModeFromCookie(context);

            if (value.HasValue())
            {
                cookie.Add(name, value, true);
            }
            else
            {
                cookie.Remove(name);
            }

            cookies.Delete(CookieNames.PreviewModeOverride);

            if (cookie.Count == 0)
            {
                // Nothing to append
                return;
            }

            cookies.Append(cookieName, cookie.ToString().TrimStart('?'), new CookieOptions
            {
                Expires = DateTime.UtcNow.AddMinutes(20),
                HttpOnly = true,
                IsEssential = true
            });
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