using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Net
{
    public static partial class WebHelper
    {
        private static IFileSystem _webRoot;
        private static IHttpContextAccessor _httpContextAccessor;
        private static PathString? _webBasePath;

        private static readonly AsyncLock _asyncLock = new();
        private static readonly Regex _htmlPathPattern = new(@"(?<=(?:href|src)=(?:""|'))(?!https?://)(?<url>[^(?:""|')]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex _cssPathPattern = new(@"url\('(?<url>.+)'\)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly ConcurrentDictionary<int, string> _safeLocalHostNames = new();

        private static IHttpContextAccessor HttpContextAccessor 
            => _httpContextAccessor ??= EngineContext.Current.Application.Services.ResolveOptional<IHttpContextAccessor>();

        /// <summary>
        /// Gets or sets the file system provider pointing at the path that contains web-servable application content files (wwwroot).
        /// </summary>
        public static IFileSystem WebRoot
        {
            get => _webRoot ??= EngineContext.Current.Application.WebRoot;
            set => _webRoot = value;
        }

        /// <summary>
        /// Gets the base path for the hosted application, e.g. <c>/myshop</c>. Returns <see cref="PathString.Empty"/>
        /// if application is not based.
        /// </summary>
        public static PathString WebBasePath
        {
            get
            {
                if (_webBasePath == null)
                {
                    if (!CommonHelper.IsHosted)
                    {
                        _webBasePath = PathString.Empty;
                    }
                    else
                    {
                        // We assume that HttpRequest.PathBase is always the same.
                        var request = HttpContextAccessor?.HttpContext?.Request;
                        if (request != null)
                        {
                            _webBasePath = request.PathBase;
                        }
                    }
                }

                return _webBasePath ?? PathString.Empty;
            }
        }

        /// <summary>
        /// Checks whether given <paramref name="path"/> is relative to the application.
        /// A relative path starts with <c>~/</c> or <c>/</c> and does not start with <see cref="WebBasePath"/>.
        /// </summary>
        /// <param name="path">Path to check.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAppRelativePath(string path)
        {
            return !IsAbsolutePath(path, out _);
        }

        /// <summary>
        /// Checks whether given <paramref name="path"/> starts with <see cref="WebBasePath"/>.
        /// Always returns <c>false</c> if <paramref name="path"/> is <c>null</c>, <see cref="string.Empty"/>
        /// or <see cref="WebBasePath"/> is empty.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="relativePath">If given path is absolute, contains the remaing path segment (which forms the actual relative path).</param>
        public static bool IsAbsolutePath(string path, out PathString relativePath)
        {
            relativePath = default;

            if (string.IsNullOrEmpty(path) || path[0] == '~' || path[0] != '/')
            {
                return false;
            }

            var applicationPath = WebBasePath;
            if (!applicationPath.HasValue)
            {
                return false;
            }

            if (path.Length >= applicationPath.Value.Length)
            {
                var segment = new PathString(path);
                if (segment.StartsWithSegments(path, StringComparison.InvariantCultureIgnoreCase, out var remaining))
                {
                    relativePath = remaining;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts an absolute path (starting with <see cref="WebBasePath"/>) to an application relative path.
        /// E.g.: /myshop/home --> ~/home.
        /// </summary>
        /// <remarks>
        /// If the specified path is not absolute, this method returns <paramref name="path"/> unchanged.
        /// </remarks>
        /// <param name="path">The absolute path to convert.</param>
        /// <returns>The relative path.</returns>
        public static string ToAppRelativePath(string path)
        {
            if (IsAbsolutePath(path, out var relativePath))
            {
                return '~' + relativePath.Value;
            }
            
            return path.EnsureStartsWith("~/");
        }

        /// <summary>
        /// Converts a virtual (relative, starting with ~/) path to an application absolute path.
        /// /// E.g.: ~/home --> /myshop/home.
        /// </summary>
        /// <remarks>
        /// If the specified path does not start with the tilde (~) character,
        /// this method returns <paramref name="path"/> unchanged.
        /// </remarks>
        /// <param name="path">The virtual path to convert.</param>
        /// <returns>The absolute path.</returns>
        public static string ToAbsolutePath(string path)
        {
            if (!string.IsNullOrEmpty(path) && path[0] == '~')
            {
                var segment = new PathString(path.Substring(1));
                var applicationPath = WebBasePath;

                return applicationPath.Add(segment).Value;
            }

            return path;
        }

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
                return baseUrl + url.EnsureStartsWith('/');
            }

            html = _htmlPathPattern.Replace(html, evaluator);
            html = _cssPathPattern.Replace(html, evaluator);

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

            path = request.PathBase + path.EnsureStartsWith('/');
            path = string.Format("{0}://{1}{2}", protocol, request.Host.Value, path);

            return path;
        }

        /// <summary>
        /// Checks whether the specified <paramref name="url"/> points to the local computer.
        /// </summary>
        /// <param name="url">URL to check.</param>
        public static bool IsUrlLocalToHost(string url)
        {
            return !url.IsEmpty() &&
                   ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) || // "/" or "/foo" but not "//" or "/\"
                    (url.Length > 1 && url[0] == '~' && url[1] == '/')); // "~/" or "~/foo"
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
                var url = string.Format("{0}://{1}{2}",
                    requestUri.Scheme,
                    requestUri.IsDefaultPort ? safeHostName : safeHostName + ":" + requestUri.Port,
                    requestUri.PathAndQuery);
                uri = new Uri(url);
            }

            var request = WebRequest.CreateHttp(uri);
            request.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
            request.ServicePoint.Expect100Continue = false;

            request.UserAgent = "Smartstore";

            return request;
        }

        private static async Task<string> GetSafeLocalHostNameAsync(Uri requestUri)
        {
            if (_safeLocalHostNames.TryGetValue(requestUri.Port, out var host))
            {
                return host;
            }

            using (await _asyncLock.LockAsync())
            {
                if (_safeLocalHostNames.TryGetValue(requestUri.Port, out host))
                {
                    return host;
                }

                var safeHost = await TestHostsAsync(requestUri.Port);
                _safeLocalHostNames.TryAdd(requestUri.Port, safeHost);

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
    }
}