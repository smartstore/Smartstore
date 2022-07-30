namespace Smartstore.Engine.Runtimes
{
    public class InstallNativePackageRequest
    {
        /// <summary>
        /// Creates a new <see cref="InstallNativePackageRequest"/> instance.
        /// </summary>
        /// <param name="libraryName">The extension-less file name of the library or executable, e.g. "libsass", "ffmpeg" etc.</param>
        /// <param name="isExecutable">Whether given <paramref name="libraryName"/> is an executable or a dynamic library.</param>
        /// <param name="packageId">The NuGet package id that contains <paramref name="libraryName"/> file.</param>
        public InstallNativePackageRequest(string libraryName, bool isExecutable, string packageId)
        {
            Guard.NotEmpty(libraryName, nameof(libraryName));
            Guard.NotEmpty(packageId, nameof(packageId));

            LibraryName = libraryName;
            IsExecutable = isExecutable;
            PackageId = packageId;
        }

        public string LibraryName { get; }
        public bool IsExecutable { get; }

        public string PackageId { get; }

        /// <summary>
        /// Whether to append the current runtime identifier (win-x64, linux-x64 etc.) to the package id automatically. Default: <c>true</c>.
        /// </summary>
        public bool AppendRIDToPackageId { get; set; } = true;

        public string MinVersion { get; set; }
        public string MaxVersion { get; set; }
    }
}
