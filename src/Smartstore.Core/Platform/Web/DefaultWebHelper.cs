using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.Collections;
using Smartstore.Engine;
using Smartstore.Utilities;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Web
{
    public partial class DefaultWebHelper : IWebHelper
    {
        private readonly static string[] _ipHeaderNames = new string[]
        {
            "HTTP_X_FORWARDED_FOR",
            "HTTP_X_FORWARDED",
            "X-FORWARDED-FOR",
            "HTTP_CF_CONNECTING_IP",
            "CF_CONNECTING_IP",
            "HTTP_CLIENT_IP",
            "HTTP_X_CLUSTER_CLIENT_IP",
            "HTTP_FORWARDED_FOR",
            "HTTP_FORWARDED",
            "REMOTE_ADDR"
        };

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly Work<IStoreContext> _storeContext;

        private bool? _isCurrentConnectionSecured;
        private IPAddress _ipAddress;

        public DefaultWebHelper(
            IHttpContextAccessor httpContextaccessor,
            IHostApplicationLifetime hostApplicationLifetime,
            Work<IStoreContext> storeContext)
        {
            _httpContextAccessor = httpContextaccessor;
            _hostApplicationLifetime = hostApplicationLifetime;
            _storeContext = storeContext;
        }

        public virtual IPAddress GetClientIpAddress()
        {
            if (_ipAddress != null)
            {
                return _ipAddress;
            }

            var context = _httpContextAccessor.HttpContext;
            var request = context?.Request;
            if (request == null)
            {
                return (_ipAddress = IPAddress.None);
            }

            IPAddress result = null;

            var headers = request.Headers;
            if (headers != null)
            {
                var keysToCheck = _ipHeaderNames;

                foreach (var key in keysToCheck)
                {
                    if (result != null)
                    {
                        break;
                    }
                    
                    if (headers.TryGetValue(key, out var ipString))
                    {
                        // Iterate list from end to start (IPv6 addresses usually have precedence)
                        for (int i = ipString.Count - 1; i >= 0; i--)
                        {
                            ipString = ipString[i].Trim();

                            if (IPAddress.TryParse(ipString, out var address))
                            {
                                result = address;
                                break;
                            }
                            else
                            {
                                // "TryParse" doesn't support IPv4 with port number
                                string str = ipString;
                                if (str.HasValue())
                                {
                                    var firstPart = str.Tokenize(':').FirstOrDefault().ToString();
                                    if (firstPart != str && IPAddress.TryParse(firstPart, out address))
                                    {
                                        result = address;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (result == null && context.Connection.RemoteIpAddress != null)
            {
                result = context.Connection.RemoteIpAddress;
            }

            if (result != null && result.AddressFamily == AddressFamily.InterNetworkV6)
            {
                result = result == IPAddress.IPv6Loopback
                    ? IPAddress.Loopback
                    : result.MapToIPv4();
            }

            return (_ipAddress = (result ?? IPAddress.None));
        }

        public virtual string GetUrlReferrer()
        {
            return _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.Referer] ?? string.Empty;
        }

        public virtual string GetClientIdent()
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = _httpContextAccessor.HttpContext?.Request?.UserAgent().EmptyNull();

            if (ipAddress != IPAddress.None && userAgent.HasValue())
            {
                return (ipAddress.ToString() + userAgent).GetHashCode().ToString();
            }

            return null;
        }

        public virtual bool IsCurrentConnectionSecured()
        {
            return _isCurrentConnectionSecured ??= _httpContextAccessor.HttpContext?.Request?.IsSecureConnection() == true;
        }

        public virtual string GetStoreLocation(bool? secured = null)
        {
            secured ??= IsCurrentConnectionSecured();

            string location;

            if (TryGetHostFromHttpContext(secured.Value, out var host))
            {
                location = host + _httpContextAccessor.HttpContext.Request.PathBase;
            }
            else
            {
                var currentStore = _storeContext.Value.CurrentStore;
                location = currentStore.GetHost(secured.Value);
            }

            return location.EnsureEndsWith('/');
        }

        protected virtual bool TryGetHostFromHttpContext(bool secured, out string host)
        {
            host = null;

            var hostHeader = _httpContextAccessor.HttpContext?.Request?.Headers?[HeaderNames.Host] ?? StringValues.Empty;

            if (!StringValues.IsNullOrEmpty(hostHeader))
            {
                host = (secured ? Uri.UriSchemeHttps : Uri.UriSchemeHttp)
                    + Uri.SchemeDelimiter
                    + hostHeader[0];

                return true;
            }

            return false;
        }

        public virtual string GetCurrentPageUrl(bool withQueryString = false, bool? secured = null, bool lowercaseUrl = false)
        {
            var httpRequest = _httpContextAccessor.HttpContext?.Request;
            if (httpRequest == null)
            {
                return string.Empty;
            }

            var storeLocation = GetStoreLocation(secured ?? IsCurrentConnectionSecured());

            var url = storeLocation.TrimEnd('/') + httpRequest.Path;

            if (withQueryString)
            {
                url += httpRequest.QueryString;
            }
            
            if (lowercaseUrl)
            {
                url = url.ToLowerInvariant();
            }

            return url;
        }

        public virtual bool IsStaticResource(HttpRequest request)
        {
            // TODO: (core) Implement (Smart)FileExtensionContentTypeProvider with more extensions
            throw new NotImplementedException();
        }

        public virtual string MapPath(string path)
        {
            return CommonHelper.MapPath(path, false);
        }

        public virtual T QueryString<T>(string name)
        {
            var queryParam = _httpContextAccessor.HttpContext?.Request?.Query["name"] ?? StringValues.Empty;

            if (!StringValues.IsNullOrEmpty(queryParam))
            {
                return queryParam.Convert<T>();
            }

            return default;
        }

        public string ModifyQueryString(string url, string queryStringModification, string anchor = null)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (string.IsNullOrEmpty(queryStringModification) && string.IsNullOrEmpty(anchor))
                return url;

            url = url.EmptyNull();
            queryStringModification = queryStringModification.EmptyNull();

            string curAnchor = null;

            var hsIndex = url.LastIndexOf('#');
            if (hsIndex >= 0)
            {
                curAnchor = url[hsIndex..];
                url = url.Substring(0, hsIndex);
            }

            var parts = url.Split(new[] { '?' });
            var current = new MutableQueryCollection(parts.Length == 2 ? parts[1] : string.Empty);
            var modify = new MutableQueryCollection(queryStringModification.EnsureStartsWith('?'));

            foreach (var nv in modify.Keys)
            {
                current.Add(nv, modify[nv], false);
            }

            var result = string.Concat(
                parts[0],
                current.ToString(),
                anchor.NullEmpty() == null ? (curAnchor == null ? "" : "#" + curAnchor) : "#" + anchor
            );

            return result;
        }

        public string RemoveQueryParam(string url, string queryParam)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (string.IsNullOrEmpty(queryParam))
                return url;

            var parts = url.SplitSafe("?").ToArray();

            var current = new MutableQueryCollection(parts.Length == 2 ? parts[1] : string.Empty);

            if (current.Count > 0 && queryParam.HasValue())
            {
                current.Remove(queryParam);
            }

            var result = parts[0] + current.ToString();

            return result;
        }

        public virtual string GetHttpHeader(string name)
        {
            Guard.NotEmpty(name, nameof(name));

            var values = StringValues.Empty;
            if (_httpContextAccessor.HttpContext?.Request?.Headers?.TryGetValue(name, out values) == true)
            {
                return values.ToString();
            }

            return string.Empty;
        }

        public virtual void RestartAppDomain()
        {
            _hostApplicationLifetime.StopApplication();
        }
    }
}