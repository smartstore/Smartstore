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
        /// Creates a transient instance of the native library installer.
        /// </summary>
        INativeLibraryInstaller CreateLibraryInstaller();
    }
}
