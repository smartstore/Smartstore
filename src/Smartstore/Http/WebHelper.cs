using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Autofac;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Http
{
    public static partial class WebHelper
    {
        private static IFileSystem _webRoot;
        private static IHttpContextAccessor _httpContextAccessor;
        private static PathString? _webBasePath;
        private static Lazy<int> _resolvedHttpsPort = new(TryResolveHttpsPort);

        private static readonly CompositeFormat _formatUrl = CompositeFormat.Parse("{0}://{1}{2}");
        private static readonly AsyncLock _asyncLock = new();
        private static readonly Regex _htmlPathPattern = new(@"(?<=(?:href|src)=(?:""|'))(?<url>[^(?:""|')]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
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
                if (segment.StartsWithSegments(applicationPath, StringComparison.InvariantCultureIgnoreCase, out var remaining))
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
                var segment = new PathString(path[1..]);
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
            Guard.NotEmpty(html);
            Guard.NotEmpty(protocol);
            Guard.NotEmpty(host);

            var scheme = protocol.TrimEnd('/').TrimEnd(':');
            var baseUrl = scheme + "://" + host.TrimEnd('/');

            string evaluator(Match match)
            {
                var url = match.Groups["url"].Value;

                if (url.StartsWith("http") || url.StartsWith("mailto:") || url.StartsWith("javascript:"))
                {
                    return url;
                }
                else if (url.StartsWith("//"))
                {
                    return scheme + ':' + url;
                }
                else
                {
                    return baseUrl + url.EnsureStartsWith('/');
                }
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
            Guard.NotEmpty(path);
            Guard.NotNull(request);

            if (path.Contains(Uri.SchemeDelimiter))
            {
                return path;
            }

            if (!request.Host.HasValue)
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

            path = ToAbsolutePath(path).EnsureStartsWith('/'); // request.PathBase + path.EnsureStartsWith('/');
            path = _formatUrl.FormatInvariant(protocol, request.Host.Value, path);

            return path;
        }

        /// <summary>
        /// Gets a valid file name from an URL.
        /// </summary>
        /// <param name="url">URL.</param>
        /// <returns>Valid file name, otherwise <c>null</c>.</returns>
        public static string GetFileNameFromUrl(string url)
        {
            string localPath = url;
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                // Exclude query string parts!
                localPath = uri.LocalPath;
            }

            var fileName = PathUtility.SanitizeFileName(Path.GetFileName(localPath).UrlDecode())
                .NullEmpty();

            return fileName;
        }

        /// <summary>
        /// Returns a value that indicates whether the URL is local. A URL is considered local if it does not have a
        /// host / authority part and it has an absolute path. URLs using virtual paths ('~/') are also local.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns><c>true</c> if the URL is local; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <para>
        /// For example, the following URLs are considered local:
        /// <code>
        /// /Views/Default/Index.html
        /// ~/Index.html
        /// </code>
        /// </para>
        /// <para>
        /// The following URLs are non-local:
        /// <code>
        /// ../Index.html
        /// http://www.contoso.com/
        /// http://localhost/Index.html
        /// </code>
        /// </para>
        /// </example>
        public static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            // Allows "/" or "/foo" but not "//" or "/\".
            if (url[0] == '/')
            {
                // url is exactly "/"
                if (url.Length == 1)
                {
                    return true;
                }

                // url doesn't start with "//" or "/\"
                if (url[1] != '/' && url[1] != '\\')
                {
                    return !HasControlCharacter(url.AsSpan(1));
                }

                return false;
            }

            // Allows "~/" or "~/foo" but not "~//" or "~/\".
            if (url[0] == '~' && url.Length > 1 && url[1] == '/')
            {
                // url is exactly "~/"
                if (url.Length == 2)
                {
                    return true;
                }

                // url doesn't start with "~//" or "~/\"
                if (url[2] != '/' && url[2] != '\\')
                {
                    return !HasControlCharacter(url.AsSpan(2));
                }

                return false;
            }

            return false;

            static bool HasControlCharacter(ReadOnlySpan<char> readOnlySpan)
            {
                // URLs may not contain ASCII control characters.
                for (var i = 0; i < readOnlySpan.Length; i++)
                {
                    if (char.IsControl(readOnlySpan[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Checks whether <paramref name="path"/> points to an extension resource (module or theme).
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="extensionType">Type of extension</param>
        /// <param name="extensionName">Name of extension (segment after "/modules" or "/themes") without slashes</param>
        /// <param name="remainingPath">The remaining segment after <paramref name="extensionName"/> including leading slash.</param>
        public static bool IsExtensionPath(string path, out ExtensionType? extensionType, out string extensionName, out string remainingPath)
        {
            extensionType = null;
            extensionName = null;
            remainingPath = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var firstChar = path.Length > 1 && (path[0] == Path.AltDirectorySeparatorChar || path[0] == Path.DirectorySeparatorChar)
                ? path[1]
                : path[0];

            if (firstChar is not ('M' or 'T'))
            {
                return false;
            }

            var tokenizer = new StringTokenizer(path.Trim(PathUtility.PathSeparators), PathUtility.PathSeparators);
            int i = 0;

            foreach (var segment in tokenizer)
            {
                if (i == 0)
                {
                    if ("Modules".Equals(segment.Value))
                    {
                        extensionType = ExtensionType.Module;
                    }
                    else if ("Themes".Equals(segment.Value))
                    {
                        extensionType = ExtensionType.Theme;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (i == 1)
                {
                    extensionName = segment.Value;
                }

                if (i == 2)
                {
                    remainingPath = segment.Buffer[(segment.Offset - 1)..];
                    break;
                }

                i++;
            }

            return !string.IsNullOrEmpty(extensionName) && !string.IsNullOrEmpty(remainingPath);
        }

        /// <summary>
        /// Gets the server's HTTPS port by looking up HTTPS_PORT environment variable,
        /// then IServerAddressesFeature.
        /// </summary>
        /// <returns>
        /// The HTTPS port or -1 if resolution failed.
        /// </returns>
        public static int GetServerHttpsPort()
            => _resolvedHttpsPort.Value;

        private static int TryResolveHttpsPort()
        {
            var appContext = EngineContext.Current?.Application;
            if (appContext == null)
            {
                return -1;
            }

            var config = appContext.Configuration;
            if (config != null)
            {
                var port = GetIntConfigValue(config, "HTTPS_PORT") ?? GetIntConfigValue(config, "ANCM_HTTPS_PORT");
                if (port.HasValue)
                {
                    return port.Value;
                }
            }

            if (appContext.Services.TryResolve<IServer>(out var server))
            {
                var serverAddressFeature = server.Features.Get<IServerAddressesFeature>();
                if (serverAddressFeature == null)
                {
                    return -1;
                }

                foreach (var address in serverAddressFeature.Addresses)
                {
                    var bindingAddress = BindingAddress.Parse(address);
                    if (bindingAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        return bindingAddress.Port;
                    }
                }
            }

            return -1;

            static int? GetIntConfigValue(IConfiguration config, string name) =>
                int.TryParse(config[name], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        public static async Task<Uri> CreateUriForSafeLocalCallAsync(Uri requestUri)
        {
            Guard.NotNull(requestUri);

            var safeHostName = await GetSafeLocalHostNameAsync(requestUri);

            if (!requestUri.Host.Equals(safeHostName, StringComparison.OrdinalIgnoreCase))
            {
                var url = _formatUrl.FormatInvariant(
                    requestUri.Scheme,
                    requestUri.IsDefaultPort ? safeHostName : safeHostName + ":" + requestUri.Port,
                    requestUri.PathAndQuery);
                return new Uri(url);
            }
            else
            {
                return requestUri;
            }
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

                // Don't obtain from factory: risk of mem leak if caller is IHostedService.
                using var httpClient = new HttpClient(new HttpClientHandler(), true)
                {
                    Timeout = TimeSpan.FromSeconds(5),
                };
                httpClient.DefaultRequestHeaders.ExpectContinue = true;

                var safeHost = await TestHostsAsync(httpClient, requestUri.Port);
                _safeLocalHostNames.TryAdd(requestUri.Port, safeHost);

                return safeHost;
            }

            async Task<string> TestHostsAsync(HttpClient httpClient, int port)
            {
                // First try original host
                if (await TestHostAsync(requestUri, requestUri.Host, httpClient))
                {
                    return requestUri.Host;
                }
                
                // Try loopback
                var hostName = Dns.GetHostName();
                var hosts = new List<string> { "localhost", hostName, "127.0.0.1" };
                foreach (var host in hosts)
                {
                    if (await TestHostAsync(requestUri, host, httpClient))
                    {
                        return host;
                    }
                }

                // Try local IP addresses
                hosts.Clear();
                var ipAddresses = Dns.GetHostAddresses(hostName).Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.ToString());
                hosts.AddRange(ipAddresses);

                foreach (var host in hosts)
                {
                    if (await TestHostAsync(requestUri, host, httpClient))
                    {
                        return host;
                    }
                }

                // None of the hosts are callable. WTF?
                return requestUri.Host;
            }
        }

        private static async Task<bool> TestHostAsync(Uri originalUri, string host, HttpClient httpClient)
        {
            var url = _formatUrl.FormatInvariant(
                originalUri.Scheme,
                originalUri.IsDefaultPort ? host : host + ":" + originalUri.Port,
                "/taskscheduler/noop");
            var uri = new Uri(url);

            try
            {
                using var response = await httpClient.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Try the next host
            }

            return false;
        }
    }
}