using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Smartstore.IO;

namespace Smartstore.Engine.Modularity.NuGet
{
    internal class NuGetExplorer : Disposable
    {
        private readonly static Lock _lock = new();

        private readonly IApplicationContext _appContext;
        private readonly SourceCacheContext _sourceCacheContext;
        private readonly HttpSourceCacheContext _cacheContext;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly NuGetLogger _nuLogger;
        private readonly Uri _indexUri;
        private readonly string _packageDownloadPath;

        private HttpSource _httpSource;
        private ServiceIndexResourceV3 _serviceIndex;

        public NuGetExplorer(
            IApplicationContext appContext,
            SourceCacheContext sourceCacheContext,
            Microsoft.Extensions.Logging.ILogger logger)
        {
            _indexUri = new Uri("https://api.nuget.org/v3/index.json");
            _packageDownloadPath = appContext.GetTempDirectory().PhysicalPath;

            _appContext = Guard.NotNull(appContext);
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _nuLogger = new NuGetLogger(_logger);
            _sourceCacheContext = sourceCacheContext ?? new SourceCacheContext();
            _cacheContext = HttpSourceCacheContext.Create(_sourceCacheContext, 5);

            if (OfflinePackageFolder == null)
            {
                lock (_lock)
                {
                    OfflinePackageFolder ??= GetOfflinePackageFolder();
                }
            }
        }

        internal static Settings EmptySettings => new(Path.GetTempPath(), "NuGet.config", false);

        /// <summary>
        /// Full physical path to the nuget local package directory, 
        /// usually "C:\Users\{User}\.nuget\packages" or "App_Data\.nuget\packages".
        /// </summary>
        private static string OfflinePackageFolder { get; set; }

        private string GetOfflinePackageFolder()
        {
            try
            {
                var path = SettingsUtility.GetGlobalPackagesFolder(NullSettings.Instance);
                var dir = LocalFolderUtility.GetAndVerifyRootDirectory(path);
                var isUnderneathRoot = dir.FullName.Replace('\\', '/').StartsWithNoCase(_appContext.ContentRoot.Root.Replace('\\', '/'));
                if (isUnderneathRoot)
                {
                    // If the global packages folder is under the app root, use the dedicated app local package folder instead.
                    _logger.Info("Using app local package folder 'App_Data/.nuget/packages'.");
                    return GetAppLocalPackageFolder().PhysicalPath;
                }

                if (!dir.Exists)
                {
                    dir.Create();
                }

                // Check write permissions
                var permissionChecker = new FilePermissionChecker(_appContext.OSIdentity);
                if (!permissionChecker.CanAccess(dir, FileEntryRights.Write | FileEntryRights.Modify | FileEntryRights.Delete))
                {
                    throw new UnauthorizedAccessException();
                }

                _logger.Info($"Using global NuGet packages folder: {dir.FullName}");

                return dir.FullName;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while resolving global NuGet packages folder. Falling back to app local folder 'App_Data/.nuget/packages'.");
                return GetAppLocalPackageFolder().PhysicalPath;
            }
        }

        private IDirectory GetAppLocalPackageFolder()
        {
            var dir = _appContext.AppDataRoot.GetDirectory(".nuget/packages");
            dir.Create();

            Environment.SetEnvironmentVariable("NUGET_PACKAGES", dir.PhysicalPath);
            Environment.SetEnvironmentVariable("NUGET_CONFIG_FILE", Path.Combine(dir.Parent.PhysicalPath, "NuGet.config"));
            //Environment.SetEnvironmentVariable("NUGET_HTTP_CACHE_PATH", @"D:\NuGet\Cache");

            return dir;
        }

        /// <summary>
        /// Tries to locate the nuget package <paramref name="id"/> in the local packages folder <see cref="OfflinePackageFolder"/>.
        /// Returns <c>null</c> if package does not exist or no version satisfies <paramref name="minVersion"/> and <paramref name="maxVersion"/>.
        /// </summary>
        /// <param name="id">Id of package to locate.</param>
        /// <param name="minVersion">Minimum version of package (optional).</param>
        /// <param name="maxVersion">Maximum version of package (optional).</param>
        /// <returns>Found package info or <c>null</c>.</returns>
        public LocalPackageInfo FindLocalPackage(string id, string minVersion = null, string maxVersion = null)
        {
            Guard.NotEmpty(id);

            var localPackages = LocalFolderUtility
                .GetPackagesV3(OfflinePackageFolder, id, _nuLogger)
                .Where(x => !x.Identity.Version.IsPrerelease)
                .OrderByDescending(x => x.Identity.Version)
                .AsEnumerable();

            if (!localPackages.Any())
            {
                return null;
            }

            var range = BuildVersionRange(minVersion, maxVersion);
            if (range != null)
            {
                localPackages = localPackages
                    .Where(x => range.Satisfies(x.Identity.Version));
            }

            return localPackages.FirstOrDefault();
        }

        /// <summary>
        /// Tries to locate the nuget package <paramref name="id"/> in the remote repository.
        /// Returns <c>null</c> if package does not exist or no version satisfies <paramref name="minVersion"/> and <paramref name="maxVersion"/>.
        /// </summary>
        /// <param name="id">Id of package to locate.</param>
        /// <param name="minVersion">Minimum version of package (optional).</param>
        /// <param name="maxVersion">Maximum version of package (optional).</param>
        /// <returns>Found package identity or <c>null</c>.</returns>
        public async Task<PackageIdentity> FindPackageAsync(
            string id,
            string minVersion = null,
            string maxVersion = null,
            CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(id);

            await EnsureServiceIndexAsync(_indexUri, cancelToken);

            var baseUri = _serviceIndex.GetPackageBaseAddressUri();
            var index = GetPackageBaseAddressIndexUri(baseUri, id);
            var json = await _httpSource.GetJObjectAsync(index, _cacheContext, _nuLogger, cancelToken);

            var versions = ((JArray)json["versions"])
                .Select(e => NuGetVersion.Parse(e.ToString()))
                .Where(x => !x.IsPrerelease)
                .OrderByDescending(x => x)
                .AsEnumerable();

            if (!versions.Any())
            {
                return null;
            }

            var range = BuildVersionRange(minVersion, maxVersion);
            if (range != null)
            {
                versions = versions
                    .Where(x => range.Satisfies(x));
            }

            var version = versions.FirstOrDefault();
            if (version == null)
            {
                return null;
            }

            return new PackageIdentity(id, version);
        }

        private static Uri GetPackageBaseAddressIndexUri(Uri packageBaseAddress, string id)
        {
            var idFixed = id.ToLowerInvariant();
            var baseUrl = packageBaseAddress.AbsoluteUri.TrimEnd('/');

            return new Uri($"{baseUrl}/{idFixed}/index.json");
        }

        /// <summary>
        /// Ensure index.json has been loaded.
        /// </summary>
        private async Task EnsureServiceIndexAsync(Uri uri, CancellationToken cancelToken)
        {
            if (_serviceIndex == null)
            {
                EnsureHttpSource();

                var index = await _httpSource.GetJObjectAsync(_indexUri, _cacheContext, _nuLogger, cancelToken);
                var resources = (index["resources"] as JArray);

                if (resources == null)
                {
                    throw new InvalidOperationException($"{uri.AbsoluteUri} does not contain a 'resources' property. Use the root service index.json for the nuget v3 feed.");
                }

                _serviceIndex = new ServiceIndexResourceV3(index, DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Ensure <see cref="HttpSource"/> has been initialized.
        /// </summary>
        private void EnsureHttpSource()
        {
            if (_httpSource != null)
            {
                return;
            }

            var packageSource = new PackageSource(_indexUri.AbsoluteUri);

            // Originally this was: ProxyCache.Instance.GetProxy(), but on IIS ProxyCache
            // tries to write a file in 'C:\WINDOWS\system32\config\systemprofile' folder, which fails of course,
            // because IIS_IUSRS has no write permission there and we simply won't require this to happen.
            var proxy = new ProxyCache(NuGetExplorer.EmptySettings, EnvironmentVariableWrapper.Instance).GetProxy(packageSource.SourceUri);

            // Replace the handler with the proxy aware handler
            var clientHandler = new HttpClientHandler
            {
                Proxy = null, // optional: dein Proxy
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            // Setup http client handler client certificates
            if (packageSource.ClientCertificates != null)
            {
                clientHandler.ClientCertificates.AddRange(packageSource.ClientCertificates.ToArray());
            }

            // HTTP handler pipeline can be injected here, around the client handler
            HttpMessageHandler messageHandler = new ServerWarningLogHandler(clientHandler);

            if (proxy != null)
            {
                messageHandler = new ProxyAuthenticationHandler(clientHandler, HttpHandlerResourceV3.CredentialService?.Value, ProxyCache.Instance);
            }

            var innerHandler = messageHandler;

            messageHandler = new HttpSourceAuthenticationHandler(packageSource, clientHandler, HttpHandlerResourceV3.CredentialService?.Value)
            {
                InnerHandler = innerHandler
            };

            var handlerResource = new HttpHandlerResourceV3(clientHandler, messageHandler);

            _httpSource = new HttpSource(
                packageSource,
                () => Task.FromResult<HttpHandlerResource>(handlerResource),
                NullThrottle.Instance);

            if (string.IsNullOrEmpty(UserAgent.UserAgentString)
                || new UserAgentStringBuilder().Build()
                    .Equals(UserAgent.UserAgentString, StringComparison.Ordinal))
            {
                // Set the user agent string if it was not already set.
                var userAgent = new UserAgentStringBuilder($"Smartstore {SmartstoreVersion.CurrentFullVersion}");
                UserAgent.SetUserAgentString(userAgent);
            }
        }

        /// <summary>
        /// Downloads and extracts given <paramref name="package"/> to <see cref="OfflinePackageFolder"/>.
        /// </summary>
        /// <param name="package">Package to download and extract.</param>
        /// <param name="cancelToken"></param>
        public async Task<LocalPackageInfo> DownloadPackageAsync(PackageIdentity package, CancellationToken cancelToken = default)
        {
            Guard.NotNull(package);

            await EnsureServiceIndexAsync(_indexUri, cancelToken);

            var uri = GetNupkgUri(_serviceIndex.GetPackageBaseAddressUri(), package);
            var packageFilePath = Path.Combine(_packageDownloadPath, $"{package}.nupkg");

            try
            {
                // Get the nupkg source result.
                var sourceResult = await _httpSource.GetNupkgAsync(uri, _cacheContext, _nuLogger, cancelToken);

                // Open the cache file if the stream does not exist.
                var nupkgStream = sourceResult.Stream ?? File.OpenRead(sourceResult.CacheFile);

                // Copy remote stream to local file.
                using (var outStream = new FileStream(packageFilePath, FileMode.Create))
                {
                    await nupkgStream.CopyToAsync(outStream, 8192, cancelToken);
                }

                var extractionContext = new PackageExtractionContext(
                    PackageSaveMode.Defaultv3,
                    XmlDocFileSaveMode.Compress,
                    ClientPolicyContext.GetClientPolicy(EmptySettings, _nuLogger),
                    _nuLogger);

                var feedContext = new OfflineFeedAddContext(
                    packagePath: packageFilePath,
                    source: OfflinePackageFolder,
                    logger: _nuLogger,
                    throwIfSourcePackageIsInvalid: true,
                    throwIfPackageExistsAndInvalid: false,
                    throwIfPackageExists: false,
                    extractionContext: extractionContext);

                await OfflineFeedUtility.AddPackageToSource(feedContext, cancelToken);

                return LocalFolderUtility.GetPackageV3(OfflinePackageFolder, package, _nuLogger);
            }
            catch
            {
                // TODO: ErrHandling
                throw;
            }
            finally
            {
                var packageFile = new FileInfo(packageFilePath);
                if (packageFile.Exists)
                {
                    packageFile.Delete();
                }
            }
        }

        private static Uri GetNupkgUri(Uri packageBaseAddress, PackageIdentity package)
        {
            var idFixed = package.Id.ToLowerInvariant();
            var versionFixed = package.Version.ToNormalizedString().ToLowerInvariant();
            var baseUrl = packageBaseAddress.AbsoluteUri.TrimEnd('/');

            return new Uri($"{baseUrl}/{idFixed}/{versionFixed}/{idFixed}.{versionFixed}.nupkg");
        }

        internal static VersionRange BuildVersionRange(string minVersion, string maxVersion)
        {
            minVersion = minVersion?.Trim();
            maxVersion = maxVersion?.Trim();

            if (minVersion == "*")
            {
                minVersion = null;
            }

            if (maxVersion == "*")
            {
                maxVersion = null;
            }

            if (minVersion != null && maxVersion != null)
            {
                return new VersionRange(minVersion: NuGetVersion.Parse(minVersion), maxVersion: NuGetVersion.Parse(maxVersion), includeMaxVersion: true);
            }
            else if (minVersion != null)
            {
                return new VersionRange(minVersion: NuGetVersion.Parse(minVersion));
            }
            else if (maxVersion != null)
            {
                return new VersionRange(maxVersion: NuGetVersion.Parse(maxVersion), includeMaxVersion: true);
            }

            return null;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _httpSource?.Dispose();
                _sourceCacheContext.Dispose();
            }
        }
    }
}
