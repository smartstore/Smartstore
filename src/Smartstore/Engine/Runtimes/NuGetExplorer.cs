using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Smartstore.Engine.Runtimes
{
    internal class NuGetExplorer
    {
        private readonly IApplicationContext _appContext;
        private readonly SourceRepository _repository;
        private readonly SourceCacheContext _sourceCache;
        private readonly ISettings _nugetSettings;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly NuGetLogger _nugetLogger;
        private readonly string _packageDownloadPath;

        public NuGetExplorer(
            IApplicationContext appContext,
            SourceRepository repository, 
            SourceCacheContext sourceCache,
            Microsoft.Extensions.Logging.ILogger logger)
        {
            _appContext = Guard.NotNull(appContext, nameof(appContext));
            _repository = repository ?? Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            _sourceCache = sourceCache ?? new SourceCacheContext();
            _nugetSettings = Settings.LoadDefaultSettings(null);
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _nugetLogger = new NuGetLogger(_logger);
            _packageDownloadPath = _appContext.GetTempDirectory().PhysicalPath;

            OfflinePackageFolder = GetOfflinePackageFolder();
        }

        private string GetOfflinePackageFolder()
        {
            try
            {
                var path = SettingsUtility.GetGlobalPackagesFolder(_nugetSettings);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
            catch
            {
                var dir = _appContext.AppDataRoot.GetDirectory(".nuget/packages");
                dir.Create();
                return dir.PhysicalPath;

            }
        }

        /// <summary>
        /// Full physical path to the nuget local package directory, 
        /// usually "C:\Users\{User}\.nuget\packages" or "App_Data\.nuget\packages".
        /// </summary>
        public string OfflinePackageFolder { get; }

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
            Guard.NotEmpty(id, nameof(id));
            
            var localPackages = LocalFolderUtility
                .GetPackagesV3(OfflinePackageFolder, id, _nugetLogger)
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
        public async Task<PackageIdentity> FindPackageAsync(string id, string minVersion = null, string maxVersion = null, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(id, nameof(id));

            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancelToken);
            var versions = (await resource
                .GetAllVersionsAsync(id, _sourceCache, _nugetLogger, cancelToken))
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

        /// <summary>
        /// Gets a downloader for given <paramref name="package"/>.
        /// </summary>
        /// <param name="package">Package to get a downloader of.</param>
        public async Task<IPackageDownloader> GetPackageDownloader(PackageIdentity package, CancellationToken cancelToken = default)
        {
            Guard.NotNull(package, nameof(package));

            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancelToken);
            var downloader = await resource.GetPackageDownloaderAsync(package, _sourceCache, _nugetLogger, cancelToken);

            return downloader;
        }

        /// <summary>
        /// Downloads and extracts given <paramref name="package"/> to <see cref="OfflinePackageFolder"/>.
        /// </summary>
        /// <param name="package">Package to download and extract.</param>
        /// <param name="cancelToken"></param>
        public async Task<LocalPackageInfo> DownloadPackageAsync(PackageIdentity package, CancellationToken cancelToken = default)
        {
            Guard.NotNull(package, nameof(package));

            var packageFilePath = Path.Combine(_packageDownloadPath, $"{package}.nupkg");
            using var downloader = await GetPackageDownloader(package, cancelToken);

            try
            {
                await downloader.CopyNupkgFileToAsync(packageFilePath, cancelToken);

                var extractionContext = new PackageExtractionContext(
                    PackageSaveMode.Defaultv3,
                    XmlDocFileSaveMode.Compress,
                    ClientPolicyContext.GetClientPolicy(_nugetSettings, _nugetLogger),
                    _nugetLogger);

                var feedContext = new OfflineFeedAddContext(
                    packagePath: packageFilePath,
                    source: OfflinePackageFolder,
                    logger: _nugetLogger,
                    throwIfSourcePackageIsInvalid: true,
                    throwIfPackageExistsAndInvalid: false,
                    throwIfPackageExists: false,
                    extractionContext: extractionContext);

                await OfflineFeedUtility.AddPackageToSource(feedContext, cancelToken);

                return LocalFolderUtility.GetPackageV3(OfflinePackageFolder, package, _nugetLogger);
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
    }
}
