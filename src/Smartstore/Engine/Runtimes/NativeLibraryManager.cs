using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Versioning;

namespace Smartstore.Engine.Runtimes
{
    public class NativeLibraryManager : INativeLibraryManager
    {
        private readonly IApplicationContext _appContext;
        private readonly ILogger _logger;
        private NuGetExplorer _explorer;

        public NativeLibraryManager(IApplicationContext appContext, ILogger logger)
        {
            _appContext = appContext;
            _logger = logger;
        }

        private NuGetExplorer Explorer
        {
            get => LazyInitializer.EnsureInitialized(ref _explorer, () => new NuGetExplorer(_appContext, null, null, _logger));
        }

        public FileInfo GetNativeLibrary(string libraryName, string minVersion = null, string maxVersion = null)
        {
            Guard.NotEmpty(libraryName, nameof(libraryName));

            return GetNativeFileInfo(libraryName, minVersion, maxVersion, isExecutable: false);
        }

        public FileInfo GetNativeExecutable(string exeName, string minVersion = null, string maxVersion = null)
        {
            Guard.NotEmpty(exeName, nameof(exeName));

            return GetNativeFileInfo(exeName, minVersion, maxVersion, isExecutable: true);
        }

        private FileInfo GetNativeFileInfo(string fileName, string minVersion, string maxVersion, bool isExecutable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = fileName.EnsureEndsWith(isExecutable ? ".exe" : ".dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !isExecutable)
            {
                fileName = fileName.EnsureEndsWith(".so");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !isExecutable)
            {
                fileName = fileName.EnsureEndsWith(".dylib");
            }
            
            var fi = new FileInfo(Path.Combine(_appContext.RuntimeInfo.NativeLibraryDirectory, fileName));

            if (fi.Exists)
            {
                var requiredVersionRange = NuGetExplorer.BuildVersionRange(minVersion, maxVersion);
                if (requiredVersionRange != null)
                {
                    // Check version of found file
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(fi.FullName);
                    var fileVersion = fileVersionInfo.FileVersion ?? fileVersionInfo.ProductVersion;

                    if (fileVersion != null && NuGetVersion.TryParse(fileVersion, out var nugetVersion))
                    {
                        if (!requiredVersionRange.Satisfies(nugetVersion))
                        {
                            // Exisiting file's version does not meet version requirement: delete file.
                            fi.WaitForUnlockAndExecute(f => f.Delete());
                            fi.Refresh();
                        }
                    }
                }
            }
            
            return fi;
        }

        public async Task<FileInfo> InstallFromPackageAsync(InstallNativePackageRequest request, CancellationToken cancelToken = default)
        {
            Guard.NotNull(request, nameof(request));

            var packageId = request.PackageId;
            if (request.AppendRIDToPackageId)
            {
                packageId = packageId.EnsureEndsWith($".{_appContext.RuntimeInfo.RID}");
            }

            var explorer = Explorer;

            // Find local package
            var localPackage = explorer.FindLocalPackage(packageId);

            if (localPackage == null)
            {
                // Local package does not exist, check NuGet online feed
                var remotePackage = await explorer.FindPackageAsync(packageId, request.MinVersion, request.MaxVersion, cancelToken);
                if (remotePackage != null)
                {
                    // Package exists, download and extract
                    localPackage = await explorer.DownloadPackageAsync(remotePackage, cancelToken);
                }
            }

            if (localPackage != null)
            {
                // Copy native files to application folder.
                CopyRuntimeFiles(localPackage);
            }

            return GetNativeFileInfo(request.LibraryName, null, null, request.IsExecutable);
        }

        private void CopyRuntimeFiles(LocalPackageInfo package)
        {
            var destination = _appContext.RuntimeInfo.BaseDirectory;
            var packageDir = Path.GetDirectoryName(package.Path);
            var rid = _appContext.RuntimeInfo.RID;

            using var reader = package.GetReader();

            var runtimeEntries = reader.GetFiles($"runtimes/{rid}")
                .Select(x => x.Replace('/', Path.DirectorySeparatorChar))
                .ToList();

            foreach (var entry in runtimeEntries)
            {
                var source = Path.Combine(packageDir, entry);
                var target = Path.Combine(destination, entry);
                var targetDir = Path.GetDirectoryName(target);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                } 

                File.Copy(source, target, true);

                // TODO: ErrHandling
            }
        }
    }
}
