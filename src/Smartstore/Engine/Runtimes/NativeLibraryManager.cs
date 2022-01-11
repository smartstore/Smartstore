using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Smartstore.Engine.Modularity.NuGet;

namespace Smartstore.Engine.Runtimes
{
    public class NativeLibraryManager : INativeLibraryManager
    {
        private readonly IApplicationContext _appContext;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public NativeLibraryManager(IApplicationContext appContext, ILoggerFactory loggerFactory)
        {
            _appContext = appContext;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<NativeLibraryManager>();
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

        internal FileInfo GetNativeFileInfo(string fileName, string minVersion, string maxVersion, bool isExecutable)
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

        public INativeLibraryInstaller CreateLibraryInstaller()
        {
            return new NativeLibraryInstaller(_appContext, this, _loggerFactory.CreateLogger<NativeLibraryInstaller>());
        }
    }
}
