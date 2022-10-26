using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.Collections;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Utilities;

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Work<IStoreContext> _storeContext;

        private bool? _isCurrentConnectionSecured;
        private IPAddress _ipAddress;
        private bool _urlReferrerResolved;
        private Uri _urlReferrer;
        private bool _publicIpAddressResolved;
        private IPAddress _publicIpAddress;

        public DefaultWebHelper(
            IHttpContextAccessor httpContextaccessor,
            IHttpClientFactory httpClientFactory,
            Work<IStoreContext> storeContext)
        {
            _httpContextAccessor = httpContextaccessor;
            _httpClientFactory = httpClientFactory;
            _storeContext = storeContext;
        }

        public HttpContext HttpContext
        {
            get => _httpContextAccessor.HttpContext;
        }

        public virtual IPAddress GetClientIpAddress()
        {
            if (_ipAddress != null)
            {
                return _ipAddress;
            }

            var request = HttpContext?.Request;
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

                            if (TryParseIPAddress(ipString, out var address))
                            {
                                result = address;
                                break;
                            }
                        }
                    }
                }
            }

            if (result == null && HttpContext.Connection.RemoteIpAddress != null)
            {
                result = HttpContext.Connection.RemoteIpAddress;
            }

            if (result != null && result.AddressFamily == AddressFamily.InterNetworkV6)
            {
                result = result == IPAddress.IPv6Loopback
                    ? IPAddress.Loopback
                    : result.MapToIPv4();
            }

            return (_ipAddress = (result ?? IPAddress.None));
        }

        public async Task<IPAddress> GetPublicIPAddressAsync()
        {
            if (_publicIpAddressResolved)
            {
                return _publicIpAddress;
            }

            var ipString = string.Empty;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.UserAgent, "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727");

            try
            {
                try
                {
                    string response = await client.GetStringAsync("http://checkip.amazonaws.com/");
                    ipString = response.Trim();
                }
                catch
                {
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(ipString))
            {
                var checkers = new string[]
                {
                    "https://ipinfo.io/ip",
                    "https://api.ipify.org",
                    "https://icanhazip.com",
                    "https://wtfismyip.com/text",
                    "http://bot.whatismyipaddress.com/"
                };

                foreach (var checker in checkers)
                {
                    try
                    {
                        ipString = (await client.GetStringAsync(checker)).Replace("\n", "");
                        if (!string.IsNullOrEmpty(ipString))
                        {
                            break;
                        }
                    }
                    catch
                    {
                    }
                }

                if (string.IsNullOrEmpty(ipString))
                {
                    try
                    {
                        var url = "http://checkip.dyndns.org";
                        using var sr = new StreamReader(await client.GetStreamAsync(url));

                        var response = sr.ReadToEnd().Trim();
                        var a = response.Split(':');
                        var a2 = a[1][1..];
                        var a3 = a2.Split('<');
                        ipString = a3[0];
                    }
                    catch
                    {
                    }
                }
            }

            if (TryParseIPAddress(ipString, out var address))
            {
                _publicIpAddress = address;
            }

            _publicIpAddressResolved = true;

            return _publicIpAddress;
        }

        private static bool TryParseIPAddress(string ipString, out IPAddress address)
        {
            if (IPAddress.TryParse(ipString, out address))
            {
                return true;
            }
            else
            {
                // "TryParse" doesn't support IPv4 with port number
                if (ipString.HasValue())
                {
                    var firstPart = ipString.Tokenize(':').FirstOrDefault().ToString();
                    if (firstPart != ipString && IPAddress.TryParse(firstPart, out address))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual Uri GetUrlReferrer()
        {
            if (_urlReferrerResolved)
            {
                return _urlReferrer;
            }

            var referrer = HttpContext?.Request?.UrlReferrer();
            if (referrer.HasValue())
            {
                Uri.TryCreate(referrer, UriKind.RelativeOrAbsolute, out _urlReferrer);
            }

            _urlReferrerResolved = true;
            return _urlReferrer;
        }

        public virtual string GetClientIdent()
        {
            var ipAddress = GetClientIpAddress();
            var userAgent = HttpContext?.Request?.UserAgent().EmptyNull();

            if (ipAddress != IPAddress.None && userAgent.HasValue())
            {
                return (ipAddress.ToString() + userAgent).GetHashCode().ToString();
            }

            return null;
        }

        public virtual bool IsCurrentConnectionSecured()
        {
            return _isCurrentConnectionSecured ??= HttpContext?.Request?.IsSecureConnection() == true;
        }

        public virtual string GetStoreLocation(bool? secured = null)
        {
            secured ??= IsCurrentConnectionSecured();

            string location;

            if (TryGetHostFromHttpContext(secured.Value, out var host))
            {
                location = host + HttpContext.Request.PathBase;
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

            var hostHeader = HttpContext?.Request?.Headers?[HeaderNames.Host] ?? StringValues.Empty;

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
            var httpRequest = HttpContext?.Request;
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

        public virtual bool IsStaticFileRequested()
        {
            return HttpContext?.Request.IsStaticFileRequested() == true;
        }

        public virtual string MapPath(string path)
        {
            return CommonHelper.MapPath(path, false);
        }

        public virtual T QueryString<T>(string name)
        {
            var queryParam = HttpContext?.Request?.Query["name"] ?? StringValues.Empty;

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
                url = url[..hsIndex];
            }

            var parts = url.Split(new[] { '?' });
            var current = new MutableQueryCollection(parts.Length == 2 ? parts[1] : string.Empty);
            var modify = new MutableQueryCollection(queryStringModification.EnsureStartsWith('?'));

            foreach (var nv in modify.Keys)
            {
                current.Add(nv, modify[nv], true);
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

            var parts = url.SplitSafe('?').ToArray();

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
            if (HttpContext?.Request?.Headers?.TryGetValue(name, out values) == true)
            {
                return values.ToString();
            }

            return string.Empty;
        }
    }
}