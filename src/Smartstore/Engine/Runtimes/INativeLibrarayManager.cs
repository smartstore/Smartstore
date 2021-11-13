using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Engine.Runtimes
{
    /// <summary>
    /// Manages native runtime libraries and executables.
    /// </summary>
    public interface INativeLibraryManager
    {
        /// <summary>
        /// Gets file info for a given <paramref name="libraryName"/>.
        /// </summary>
        /// <param name="libraryName">
        /// The extension-less file name of the library, e.g. "libsass". The extension will be
        /// appended automatically depending on current OS and bitness (<c>.dll</c>, <c>.so</c> or <c>.dylib</c>). 
        /// 
        /// If the file was found in the designated runtime folder and <paramref name="minVersion"/> and/or <paramref name="maxVersion"/> are set,
        /// a file version check will be performed. If the file's version does not satisfy the given version range, it will be deleted.
        /// </param>
        /// <param name="minVersion">Minimum required version of library.</param>
        /// <param name="maxVersion">Maximum version of library.</param>
        /// <returns>A <see cref="FileInfo"/> instance.</returns>
        FileInfo GetNativeLibrary(string libraryName, string minVersion = null, string maxVersion = null);

        /// <summary>
        /// Gets file info for a given <paramref name="exeName"/>.
        /// </summary>
        /// <param name="exeName">
        /// The extension-less file name of the executable, e.g. "ffmpeg". The extension will be
        /// appended automatically depending on current OS and bitness (<c>.exe</c> for Windows). 
        /// 
        /// If the file was found in the designated runtime folder and <paramref name="minVersion"/> and/or <paramref name="maxVersion"/> are set,
        /// a file version check will be performed. If the file's version does not satisfy the given version range, it will be deleted.
        /// </param>
        /// <param name="minVersion">Minimum required version of executable.</param>
        /// <param name="maxVersion">Maximum version of executable.</param>
        /// <returns>A <see cref="FileInfo"/> instance.</returns>
        FileInfo GetNativeExecutable(string exeName, string minVersion = null, string maxVersion = null);

        /// <summary>
        /// Installs a native library or executable from a NuGet source in the designated runtime folder.
        /// If the NuGet package exists in the local (machine-wide) package cache, files are copied from this cache to the target. 
        /// Otherwise the package is downloaded and extracted first.
        /// </summary>
        /// <param name="request">A context object with all required information for the install process.</param>
        /// <param name="cancelToken"></param>
        /// <returns>A <see cref="FileInfo"/> instance representing the target native file in the application's runtime folder.</returns>
        Task<FileInfo> InstallFromPackageAsync(InstallNativePackageRequest request, CancellationToken cancelToken = default);
    }
}
