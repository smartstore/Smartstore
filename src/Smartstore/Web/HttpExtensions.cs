using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

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
    }
}