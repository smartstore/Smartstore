using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Smartstore.Collections;
using Smartstore.Engine;
using Smartstore.Threading;
using Smartstore.Utilities;
using Smartstore.Web;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Web
{
    public partial class WebHelper : IWebHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly Work<IStoreContext> _storeContext;

        private bool? _isCurrentConnectionSecured;
        private string _ipAddress;

        public WebHelper(
            IHttpContextAccessor httpContextaccessor,
            IHostApplicationLifetime hostApplicationLifetime,
            Work<IStoreContext> storeContext)
        {
            _httpContextAccessor = httpContextaccessor;
            _hostApplicationLifetime = hostApplicationLifetime;
            _storeContext = storeContext;
        }

        public virtual string GetCurrentIpAddress()
        {
            if (_ipAddress != null)
            {
                return _ipAddress;
            }

            var context = _httpContextAccessor.HttpContext;
            var request = context?.Request;
            if (request == null)
            {
                return string.Empty;
            }

            string result = null;
            IPAddress ipv6 = null;

            var headers = request.Headers;
            if (headers != null)
            {
                var keysToCheck = new string[]
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

                foreach (var key in keysToCheck)
                {
                    if (headers.TryGetValue(key, out var ipString))
                    {
                        // Iterate list from end to start (IPv6 addresses usually have precedence)
                        for (int i = ipString.Count - 1; i >= 0; i--)
                        {
                            ipString = ipString[i].Trim();

                            if (IPAddress.TryParse(ipString, out var address))
                            {
                                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    ipv6 = address;
                                }
                                else
                                {
                                    result = ipString;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(result) && context.Connection.RemoteIpAddress != null)
            {
                result = context.Connection.RemoteIpAddress.ToString();
            }

            if (string.IsNullOrEmpty(result) && ipv6 != null)
            {
                result = ipv6 == IPAddress.IPv6Loopback
                    ? IPAddress.Loopback.ToString()
                    : ipv6.MapToIPv4().ToString();
            }

            if (!string.IsNullOrEmpty(result) && !IPAddress.TryParse(result, out _))
            {
                // "TryParse" doesn't support IPv4 with port number
                result = result.Split(':').FirstOrDefault();
            }

            return (_ipAddress = result.EmptyNull());
        }

        public virtual string GetUrlReferrer()
        {
            return _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.Referer] ?? string.Empty;
        }

        public virtual string GetClientIdent()
        {
            var ipAddress = GetCurrentIpAddress();
            var userAgent = _httpContextAccessor.HttpContext?.Request?.UserAgent().EmptyNull();

            if (ipAddress.HasValue() && userAgent.HasValue())
            {
                return (ipAddress + userAgent).GetHashCode().ToString();
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

            return location.EnsureEndsWith("/");
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
                curAnchor = url.Substring(hsIndex);
                url = url.Substring(0, hsIndex);
            }

            var parts = url.Split(new[] { '?' });
            var current = new MutableQueryCollection(parts.Length == 2 ? parts[1] : string.Empty);
            var modify = new MutableQueryCollection(queryStringModification.EnsureStartsWith("?"));

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

            var parts = url.SplitSafe("?");

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

        #region Static Utils

        private static readonly AsyncLock s_asyncLock = new AsyncLock();
        private static readonly Regex s_htmlPathPattern = new Regex(@"(?<=(?:href|src)=(?:""|'))(?!https?://)(?<url>[^(?:""|')]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex s_cssPathPattern = new Regex(@"url\('(?<url>.+)'\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly ConcurrentDictionary<int, string> s_safeLocalHostNames = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// Prepends protocol and host to all (relative) urls in a html string
        /// </summary>
        /// <param name="html">The html string</param>
        /// <param name="request">Request object</param>
        /// <returns>The transformed result html</returns>
        /// <remarks>
        /// All html attributed named <c>src</c> and <c>href</c> are affected, also occurences of <c>url('path')</c> within embedded stylesheets.
        /// </remarks>
        public static string MakeAllUrlsAbsolute(string html, HttpRequest request)
        {
            Guard.NotNull(request, nameof(request));

            if (!request.Host.HasValue)
            {
                return html;
            }

            return MakeAllUrlsAbsolute(html, request.Scheme, request.Host.Value);
        }

        /// <summary>
        /// Prepends protocol and host to all (relative) urls in a html string
        /// </summary>
        /// <param name="html">The html string</param>
        /// <param name="protocol">The protocol to prepend, e.g. <c>http</c></param>
        /// <param name="host">The host name to prepend, e.g. <c>www.mysite.com</c></param>
        /// <returns>The transformed result html</returns>
        /// <remarks>
        /// All html attributed named <c>src</c> and <c>href</c> are affected, also occurences of <c>url('path')</c> within embedded stylesheets.
        /// </remarks>
        public static string MakeAllUrlsAbsolute(string html, string protocol, string host)
        {
            Guard.NotEmpty(html, nameof(html));
            Guard.NotEmpty(protocol, nameof(protocol));
            Guard.NotEmpty(host, nameof(host));

            string baseUrl = protocol.EnsureEndsWith("://") + host.TrimEnd('/');

            string evaluator(Match match)
            {
                var url = match.Groups["url"].Value;
                return baseUrl + url.EnsureStartsWith("/");
            }

            html = s_htmlPathPattern.Replace(html, evaluator);
            html = s_cssPathPattern.Replace(html, evaluator);

            return html;
        }

        /// <summary>
        /// Prepends protocol and host to the given (relative) url
        /// </summary>
        /// <param name="path">The relative path without base part.</param>
        /// <param name="protocol">Changes the protocol if passed.</param>
        public static string GetAbsoluteUrl(string path, HttpRequest request, bool enforceScheme = false, string protocol = null)
        {
            Guard.NotEmpty(path, nameof(path));
            Guard.NotNull(request, nameof(request));

            if (!request.Host.HasValue)
            {
                return path;
            }

            if (path.Contains(Uri.SchemeDelimiter))
            {
                return path;
            }

            protocol ??= request.Scheme;

            if (path.StartsWith("//"))
            {
                return enforceScheme
                    ? string.Concat(protocol, ":", path)
                    : path;
            }

            path = (request.PathBase + path).EnsureStartsWith("/");
            path = string.Format("{0}://{1}{2}", protocol, request.Host.Value, path);

            return path;
        }

        public static async Task<string> GetPublicIPAddressAsync()
        {
            string result = string.Empty;

            try
            {
                using var client = new WebClient();
                client.Headers[HeaderNames.UserAgent] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                try
                {
                    byte[] arr = await client.DownloadDataTaskAsync("http://checkip.amazonaws.com/");
                    string response = Encoding.UTF8.GetString(arr);
                    result = response.Trim();
                }
                catch { }
            }
            catch { }

            var checkers = new string[]
            {
                "https://ipinfo.io/ip",
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://wtfismyip.com/text",
                "http://bot.whatismyipaddress.com/"
            };

            if (string.IsNullOrEmpty(result))
            {
                using var client = new WebClient();
                foreach (var checker in checkers)
                {
                    try
                    {
                        result = (await client.DownloadStringTaskAsync(checker)).Replace("\n", "");
                        if (!string.IsNullOrEmpty(result))
                        {
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    var url = "http://checkip.dyndns.org";
                    var req = WebRequest.Create(url);

                    using var resp = await req.GetResponseAsync();
                    using var sr = new StreamReader(resp.GetResponseStream());

                    var response = sr.ReadToEnd().Trim();
                    var a = response.Split(':');
                    var a2 = a[1].Substring(1);
                    var a3 = a2.Split('<');
                    result = a3[0];
                }
                catch { }
            }

            return result;
        }

        public static async Task<HttpWebRequest> CreateHttpRequestForSafeLocalCallAsync(Uri requestUri)
        {
            Guard.NotNull(requestUri, nameof(requestUri));

            var safeHostName = await GetSafeLocalHostNameAsync(requestUri);

            var uri = requestUri;

            if (!requestUri.Host.Equals(safeHostName, StringComparison.OrdinalIgnoreCase))
            {
                var url = String.Format("{0}://{1}{2}",
                    requestUri.Scheme,
                    requestUri.IsDefaultPort ? safeHostName : safeHostName + ":" + requestUri.Port,
                    requestUri.PathAndQuery);
                uri = new Uri(url);
            }

            var request = WebRequest.CreateHttp(uri);
            request.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
            request.ServicePoint.Expect100Continue = false;

            // TODO: (core) Implement SmartStoreVersion
            //request.UserAgent = "Smartstore {0}".FormatInvariant(SmartStoreVersion.CurrentFullVersion);

            return request;
        }

        private static async Task<string> GetSafeLocalHostNameAsync(Uri requestUri)
        {
            if (s_safeLocalHostNames.TryGetValue(requestUri.Port, out var host))
            {
                return host;
            }

            using (await s_asyncLock.LockAsync())
            {
                if (s_safeLocalHostNames.TryGetValue(requestUri.Port, out host))
                {
                    return host;
                }

                var safeHost = await TestHostsAsync(requestUri.Port);
                s_safeLocalHostNames.TryAdd(requestUri.Port, safeHost);

                return safeHost;
            }

            async Task<string> TestHostsAsync(int port)
            {
                // first try original host
                if (await TestHostAsync(requestUri, requestUri.Host, 5000))
                {
                    return requestUri.Host;
                }

                // try loopback
                var hostName = Dns.GetHostName();
                var hosts = new List<string> { "localhost", hostName, "127.0.0.1" };
                foreach (var host in hosts)
                {
                    if (await TestHostAsync(requestUri, host, 500))
                    {
                        return host;
                    }
                }

                // try local IP addresses
                hosts.Clear();
                var ipAddresses = Dns.GetHostAddresses(hostName).Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.ToString());
                hosts.AddRange(ipAddresses);

                foreach (var host in hosts)
                {
                    if (await TestHostAsync(requestUri, host, 500))
                    {
                        return host;
                    }
                }

                // None of the hosts are callable. WTF?
                return requestUri.Host;
            }
        }

        private static async Task<bool> TestHostAsync(Uri originalUri, string host, int timeout)
        {
            var url = String.Format("{0}://{1}/taskscheduler/noop",
                originalUri.Scheme,
                originalUri.IsDefaultPort ? host : host + ":" + originalUri.Port);
            var uri = new Uri(url);

            var request = WebRequest.CreateHttp(uri);
            request.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
            request.ServicePoint.Expect100Continue = false;
            request.UserAgent = "Smartstore";
            request.Timeout = timeout;

            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)(await request.GetResponseAsync());
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch
            {
                // try the next host
            }
            finally
            {
                if (response != null)
                    response.Dispose();
            }

            return false;
        }

        #endregion
    }
}