using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using Smartstore.Engine.Modularity.NuGet;
using Smartstore.Utilities;

namespace Smartstore.Engine.Runtimes
{
    /// <summary>
    /// Responsinble for downloading, extracting and deploying native library files from online NuGet packages.
    /// </summary>
    public interface INativeLibraryInstaller : IDisposable
    {
        /// <summary>
        /// Installs a native library or executable from a NuGet source in the designated runtime folder.
        /// If the NuGet package exists in the local (machine-wide) package cache, files are copied from this cache to the target. 
        /// Otherwise the package is downloaded and extracted first.
        /// </summary>
        /// <param name="request">A context object with all required information for the install process.</param>
        /// <returns>A <see cref="FileInfo"/> instance representing the target native file in the application's runtime folder.</returns>
        Task<FileInfo> InstallFromPackageAsync(InstallNativePackageRequest request, CancellationToken cancelToken = default);
    }

    internal class NativeLibraryInstaller : Disposable, INativeLibraryInstaller
    {
        private readonly IApplicationContext _appContext;
        private readonly NativeLibraryManager _manager;
        private readonly NuGetExplorer _explorer;
        private readonly ILogger _logger;

        public NativeLibraryInstaller(
            IApplicationContext appContext,
            NativeLibraryManager manager,
            ILogger logger)
        {
            Guard.NotNull(appContext, nameof(appContext));
            Guard.NotNull(manager, nameof(manager));
            Guard.NotNull(logger, nameof(logger));

            _appContext = appContext;
            _manager = manager;
            _logger = logger;
            _explorer = new NuGetExplorer(appContext, null, logger);
        }

        public async Task<FileInfo> InstallFromPackageAsync(InstallNativePackageRequest request, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var packageId = request.PackageId;
            if (request.AppendRIDToPackageId)
            {
                packageId = packageId.EnsureEndsWith($".{_appContext.RuntimeInfo.RID}");
            }

            // Find local package
            var localPackage = _explorer.FindLocalPackage(packageId);

            if (localPackage == null)
            {
                // Local package does not exist, check NuGet online feed
                var remotePackage = await _explorer.FindPackageAsync(packageId, request.MinVersion, request.MaxVersion, cancelToken);
                if (remotePackage != null)
                {
                    // Package exists, download and extract
                    localPackage = await _explorer.DownloadPackageAsync(remotePackage, cancelToken);
                }
            }

            if (localPackage != null)
            {
                // Copy native files to application folder.
                CopyRuntimeFiles(localPackage, request);
            }

            return _manager.GetNativeFileInfo(request.LibraryName, null, null, request.IsExecutable);
        }

        private void CopyRuntimeFiles(LocalPackageInfo package, InstallNativePackageRequest request)
        {
            //if (request.IsExecutable)
            //{
            //    _logger.Info($"Creating runtime directory '{_appContext.RuntimeInfo.RID}'.");
            //    Directory.CreateDirectory(_appContext.RuntimeInfo.NativeLibraryDirectory);
            //}

            var destination = _appContext.RuntimeInfo.IsWindows && CommonHelper.IsDevEnvironment
                ? _appContext.RuntimeInfo.NativeLibraryDirectory
                : _appContext.RuntimeInfo.BaseDirectory;
            var packageDir = Path.GetDirectoryName(package.Path);
            var rid = _appContext.RuntimeInfo.RID;

            using var reader = package.GetReader();

            var runtimeEntries = reader.GetFiles($"runtimes/{rid}")
                .Select(x => x.Replace('/', Path.DirectorySeparatorChar))
                .ToList();

            foreach (var entry in runtimeEntries)
            {
                // Native libary files (.exe, .dll, .so, .dylib) belong to the root.
                var fileName = Path.GetFileName(entry);

                var source = Path.Combine(packageDir, entry);
                var target = Path.Combine(destination, fileName);
                var targetDir = Path.GetDirectoryName(target);

                _logger.Info($"Copy native library file from '{source}' to '{target}'.");

                File.Copy(source, target, true);

                // TODO: ErrHandling
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _explorer.Dispose();
            }
        }
    }
}
