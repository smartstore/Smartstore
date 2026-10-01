using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Smartstore.Engine.Modularity.NuGet;
using Smartstore.Utilities;

namespace Smartstore.Engine.Runtimes;

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

        var requiredVersionRange = NuGetExplorer.BuildVersionRange(minVersion, maxVersion);

        if (isExecutable && TryStartProcess(fileName, out var systemFile) && IsVersionCompatible(systemFile, requiredVersionRange))
        {
            return systemFile;
        }

        var baseDirectory = _appContext.RuntimeInfo.IsWindows && CommonHelper.IsDevEnvironment
            ? _appContext.RuntimeInfo.NativeLibraryDirectory
            : _appContext.RuntimeInfo.BaseDirectory;
        var fi = new FileInfo(Path.Combine(baseDirectory, fileName));

        if (fi.Exists)
        {
            if (IsVersionCompatible(fi, requiredVersionRange))
            {
                return fi;
            }

            // Existing file's version does not meet the requirement: delete it and its package-version sidecar.
            fi.WaitForUnlockAndExecute(f => f.Delete());
            File.Delete(GetVersionFilePath(fi.FullName));
            fi.Refresh();
        }

        return fi;
    }

    internal static string GetVersionFilePath(string filePath)
    {
        return filePath + ".version";
    }

    internal static bool IsVersionCompatible(FileInfo file, VersionRange requiredVersionRange)
    {
        if (requiredVersionRange == null)
        {
            return true;
        }

        var versionFile = GetVersionFilePath(file.FullName);
        string version;

        if (File.Exists(versionFile))
        {
            version = File.ReadAllText(versionFile).Trim();
        }
        else
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(file.FullName);
            version = versionInfo.FileVersion ?? versionInfo.ProductVersion;
        }

        return NuGetVersion.TryParse(version, out var nativeVersion) && requiredVersionRange.Satisfies(nativeVersion);
    }

    public INativeLibraryInstaller CreateLibraryInstaller()
    {
        return new NativeLibraryInstaller(_appContext, this, _loggerFactory.CreateLogger<NativeLibraryInstaller>());
    }

    internal bool TryStartProcess(string fileName, out FileInfo fi)
    {
        // Check if any machine-wide tool can be started
        fi = null;

        Process process = null;

        try
        {
            // Under binfmt/QEMU, MainModule points to the emulator, not the requested tool.
            // Resolve Unix executables before starting them so version checks use the actual file.
            var executablePath = OperatingSystem.IsWindows() ? null : ResolveUnixExecutablePath(fileName);
            if (!OperatingSystem.IsWindows() && executablePath == null)
            {
                return false;
            }

            process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath ?? fileName,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            var startedFilePath = executablePath ?? process.MainModule?.FileName;
            if (startedFilePath != null)
            {
                fi = new FileInfo(startedFilePath);
                if (fi.Exists)
                {
                    _logger.Info($"'{Path.GetFileNameWithoutExtension(fileName)}' library is ready.");
                }
            }

            return fi?.Exists == true;
        }
        catch
        {
            return false;
        }
        finally
        {
            process?.EnsureStopped();
        }
    }

    internal static string ResolveUnixExecutablePath(string fileName)
    {
        if (fileName.Contains('/'))
        {
            var path = Path.GetFullPath(fileName);
            return IsExecutable(path) ? path : null;
        }

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            var path = Path.GetFullPath(Path.Combine(directory, fileName));
            if (IsExecutable(path))
            {
                return path;
            }
        }

        return null;

        static bool IsExecutable(string path)
        {
            if (!File.Exists(path) || OperatingSystem.IsWindows())
            {
                return false;
            }

            var mode = File.GetUnixFileMode(path);
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
    }
}
