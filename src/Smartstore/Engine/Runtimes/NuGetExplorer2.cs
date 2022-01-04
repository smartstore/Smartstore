//using System;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json.Linq;
//using NuGet.Configuration;
//using NuGet.Packaging;
//using NuGet.Packaging.Core;
//using NuGet.Packaging.Signing;
//using NuGet.Protocol;
//using NuGet.Protocol.Core.Types;
//using NuGet.Versioning;

//namespace Smartstore.Engine.Runtimes
//{
//    internal class NuGetExplorer2 : Disposable
//    {
//        private readonly IApplicationContext _appContext;
//        private readonly SourceCacheContext _sourceCacheContext;
//        private readonly HttpSourceCacheContext _cacheContext;
//        private readonly ISettings _nugetSettings;
//        private readonly Microsoft.Extensions.Logging.ILogger _logger;
//        private readonly NuGetLogger _nugetLogger;

//        private readonly Uri _indexUri;
//        private readonly string _packageDownloadPath;

//        private HttpSource _httpSource;
//        private ServiceIndexResourceV3 _serviceIndex;

//        public NuGetExplorer2(
//            IApplicationContext appContext,
//            SourceCacheContext sourceCacheContext,
//            Microsoft.Extensions.Logging.ILogger logger)
//        {
//            _indexUri = new Uri("https://api.nuget.org/v3/index.json");
//            _packageDownloadPath = appContext.GetTempDirectory().PhysicalPath;

//            _appContext = Guard.NotNull(appContext, nameof(appContext));
//            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
//            _nugetLogger = new NuGetLogger(_logger);
//            _sourceCacheContext = sourceCacheContext ?? new SourceCacheContext();
//            _cacheContext = HttpSourceCacheContext.Create(_sourceCacheContext, 5);

//            OfflinePackageFolder = GetOfflinePackageFolder();
//        }

//        /// <summary>
//        /// Full physical path to the nuget local package directory, 
//        /// usually "C:\Users\{User}\.nuget\packages" or "App_Data\.nuget\packages".
//        /// </summary>
//        public string OfflinePackageFolder { get; }

//        private string GetOfflinePackageFolder()
//        {
//            try
//            {
//                var settings = Settings.LoadDefaultSettings(null);
//                var path = SettingsUtility.GetGlobalPackagesFolder(settings);
//                if (!Directory.Exists(path))
//                {
//                    Directory.CreateDirectory(path);
//                }

//                return path;
//            }
//            catch (Exception ex)
//            {
//                var dir = _appContext.AppDataRoot.GetDirectory(".nuget/packages");
//                dir.Create();

//                _logger.Error(ex, "Error while resolving NuGet global packages folder. Falling back to app wide folder '.nuget/packages'.");

//                return dir.PhysicalPath;
//            }
//        }

//        /// <summary>
//        /// Tries to locate the nuget package <paramref name="id"/> in the local packages folder <see cref="OfflinePackageFolder"/>.
//        /// Returns <c>null</c> if package does not exist or no version satisfies <paramref name="minVersion"/> and <paramref name="maxVersion"/>.
//        /// </summary>
//        /// <param name="id">Id of package to locate.</param>
//        /// <param name="minVersion">Minimum version of package (optional).</param>
//        /// <param name="maxVersion">Maximum version of package (optional).</param>
//        /// <returns>Found package info or <c>null</c>.</returns>
//        public LocalPackageInfo FindLocalPackage(string id, string minVersion = null, string maxVersion = null)
//        {
//            Guard.NotEmpty(id, nameof(id));
            
//            var localPackages = LocalFolderUtility
//                .GetPackagesV3(OfflinePackageFolder, id, _nugetLogger)
//                .Where(x => !x.Identity.Version.IsPrerelease)
//                .OrderByDescending(x => x.Identity.Version)
//                .AsEnumerable();

//            if (!localPackages.Any())
//            {
//                return null;
//            }

//            var range = BuildVersionRange(minVersion, maxVersion);
//            if (range != null)
//            {
//                localPackages = localPackages
//                    .Where(x => range.Satisfies(x.Identity.Version));
//            }

//            return localPackages.FirstOrDefault();
//        }

//        /// <summary>
//        /// Tries to locate the nuget package <paramref name="id"/> in the remote repository.
//        /// Returns <c>null</c> if package does not exist or no version satisfies <paramref name="minVersion"/> and <paramref name="maxVersion"/>.
//        /// </summary>
//        /// <param name="id">Id of package to locate.</param>
//        /// <param name="minVersion">Minimum version of package (optional).</param>
//        /// <param name="maxVersion">Maximum version of package (optional).</param>
//        /// <returns>Found package identity or <c>null</c>.</returns>
//        public async Task<PackageIdentity> FindPackageAsync(
//            string id, 
//            string minVersion = null, 
//            string maxVersion = null, 
//            CancellationToken cancelToken = default)
//        {
//            Guard.NotEmpty(id, nameof(id));

//            await EnsureServiceIndexAsync(_indexUri, cancelToken);

//            var baseUri = _serviceIndex.GetPackageBaseAddressUri();
//            var index = GetPackageBaseAddressIndexUri(baseUri, id);
//            var json = await _httpSource.GetJObjectAsync(index, _cacheContext, _nugetLogger, cancelToken);

//            var versions = ((JArray)json["versions"])
//                .Select(e => NuGetVersion.Parse(e.ToString()))
//                .Where(x => !x.IsPrerelease)
//                .OrderByDescending(x => x)
//                .AsEnumerable();

//            if (!versions.Any())
//            {
//                return null;
//            }

//            var range = BuildVersionRange(minVersion, maxVersion);
//            if (range != null)
//            {
//                versions = versions
//                    .Where(x => range.Satisfies(x));
//            }

//            var version = versions.FirstOrDefault();
//            if (version == null)
//            {
//                return null;
//            }

//            return new PackageIdentity(id, version);
//        }

//        private static Uri GetPackageBaseAddressIndexUri(Uri packageBaseAddress, string id)
//        {
//            var idFixed = id.ToLowerInvariant();
//            var baseUrl = packageBaseAddress.AbsoluteUri.TrimEnd('/');

//            return new Uri($"{baseUrl}/{idFixed}/index.json");
//        }

//        /// <summary>
//        /// Ensure index.json has been loaded.
//        /// </summary>
//        private async Task EnsureServiceIndexAsync(Uri uri, CancellationToken cancelToken)
//        {
//            if (_serviceIndex == null)
//            {
//                await EnsureHttpSourceAsync();

//                var index = await _httpSource.GetJObjectAsync(_indexUri, _cacheContext, _nugetLogger, cancelToken);
//                var resources = (index["resources"] as JArray);

//                if (resources == null)
//                {
//                    throw new InvalidOperationException($"{uri.AbsoluteUri} does not contain a 'resources' property. Use the root service index.json for the nuget v3 feed.");
//                }

//                _serviceIndex = new ServiceIndexResourceV3(index, DateTime.UtcNow);
//            }
//        }

//        /// <summary>
//        /// Ensure <see cref="HttpSource"/> has been initialized.
//        /// </summary>
//        private async Task EnsureHttpSourceAsync()
//        {
//            if (_httpSource == null)
//            {
//                var source = Repository.Factory.GetCoreV3(_indexUri.AbsoluteUri);
//                var handlerResource = await source.GetResourceAsync<HttpHandlerResource>();

//                var packageSource = new PackageSource(_indexUri.AbsoluteUri);

//                _httpSource = new HttpSource(
//                    packageSource,
//                    () => Task.FromResult(handlerResource),
//                    NullThrottle.Instance);

//                if (string.IsNullOrEmpty(UserAgent.UserAgentString)
//                    || new UserAgentStringBuilder().Build()
//                        .Equals(UserAgent.UserAgentString, StringComparison.Ordinal))
//                {
//                    // Set the user agent string if it was not already set.
//                    var userAgent = new UserAgentStringBuilder("Smartstore");
//                    UserAgent.SetUserAgentString(userAgent);
//                }
//            }
//        }

//        /// <summary>
//        /// Gets a downloader for given <paramref name="package"/>.
//        /// </summary>
//        /// <param name="package">Package to get a downloader of.</param>
//        public async Task<IPackageDownloader> GetPackageDownloader(PackageIdentity package, CancellationToken cancelToken = default)
//        {
//            Guard.NotNull(package, nameof(package));

//            //var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancelToken);
//            //var downloader = await resource.GetPackageDownloaderAsync(package, _sourceCacheContext, _nugetLogger, cancelToken);

//            await Task.Delay(100);

//            return null;
//        }

//        /// <summary>
//        /// Downloads and extracts given <paramref name="package"/> to <see cref="OfflinePackageFolder"/>.
//        /// </summary>
//        /// <param name="package">Package to download and extract.</param>
//        /// <param name="cancelToken"></param>
//        public async Task<LocalPackageInfo> DownloadPackageAsync(PackageIdentity package, CancellationToken cancelToken = default)
//        {
//            Guard.NotNull(package, nameof(package));

//            var packageFilePath = Path.Combine(_packageDownloadPath, $"{package}.nupkg");
//            using var downloader = await GetPackageDownloader(package, cancelToken);

//            try
//            {
//                await downloader.CopyNupkgFileToAsync(packageFilePath, cancelToken);

//                var extractionContext = new PackageExtractionContext(
//                    PackageSaveMode.Defaultv3,
//                    XmlDocFileSaveMode.Compress,
//                    ClientPolicyContext.GetClientPolicy(_nugetSettings, _nugetLogger),
//                    _nugetLogger);

//                var feedContext = new OfflineFeedAddContext(
//                    packagePath: packageFilePath,
//                    source: OfflinePackageFolder,
//                    logger: _nugetLogger,
//                    throwIfSourcePackageIsInvalid: true,
//                    throwIfPackageExistsAndInvalid: false,
//                    throwIfPackageExists: false,
//                    extractionContext: extractionContext);

//                await OfflineFeedUtility.AddPackageToSource(feedContext, cancelToken);

//                return LocalFolderUtility.GetPackageV3(OfflinePackageFolder, package, _nugetLogger);
//            }
//            catch
//            {
//                // TODO: ErrHandling
//                throw;
//            }
//            finally
//            {
//                var packageFile = new FileInfo(packageFilePath);
//                if (packageFile.Exists)
//                {
//                    packageFile.Delete();
//                }
//            }
//        }

//        internal static VersionRange BuildVersionRange(string minVersion, string maxVersion)
//        {
//            minVersion = minVersion?.Trim();
//            maxVersion = maxVersion?.Trim();

//            if (minVersion == "*")
//            {
//                minVersion = null;
//            }

//            if (maxVersion == "*")
//            {
//                maxVersion = null;
//            }

//            if (minVersion != null && maxVersion != null)
//            {
//                return new VersionRange(minVersion: NuGetVersion.Parse(minVersion), maxVersion: NuGetVersion.Parse(maxVersion), includeMaxVersion: true);
//            }
//            else if (minVersion != null)
//            {
//                return new VersionRange(minVersion: NuGetVersion.Parse(minVersion));
//            }
//            else if (maxVersion != null)
//            {
//                return new VersionRange(maxVersion: NuGetVersion.Parse(maxVersion), includeMaxVersion: true);
//            }

//            return null;
//        }

//        protected override void OnDispose(bool disposing)
//        {
//            if (disposing)
//            {
//                _httpSource?.Dispose();
//                _sourceCacheContext.Dispose();
//            }
//        }
//    }
//}
