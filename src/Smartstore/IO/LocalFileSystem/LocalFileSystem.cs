using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using Smartstore.Http;
using Smartstore.Utilities;

namespace Smartstore.IO
{
    /// <summary>
    /// Looks up files using the on-disk file system
    /// </summary>
    /// <remarks>
    /// When the environment variable "DOTNET_USE_POLLING_FILE_WATCHER" is set to "1" or "true", calls to
    /// <see cref="Watch(string)" /> will use <see cref="PollingFileChangeToken" />.
    /// </remarks>
    public class LocalFileSystem : FileSystemBase, IFileProvider
    {
        // Defaults are: AttributesToSkip = FileAttributes.Hidden | FileAttributes.System, IgnoreInaccessible = true
        private static readonly EnumerationOptions _flatEnumerationOptions = new();
        private static readonly EnumerationOptions _deepEnumerationOptions = new() { RecurseSubdirectories = true };

        private static readonly char[] PathSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private readonly PhysicalFileProvider _provider;
        private readonly ExclusionFilters _filters;

        /// <summary>
        /// Initializes a new instance of a LocalFileSystem with the given physical root path.
        /// </summary>
        public LocalFileSystem(string root)
        {
            Guard.NotEmpty(root, nameof(root));

            _filters = ExclusionFilters.Sensitive;
            _provider = new PhysicalFileProvider(root, _filters);
        }

        #region IFileProvider

        public override IFileInfo GetFileInfo(string subpath)
        {
            return _provider.GetFileInfo(subpath);
        }

        public override IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _provider.GetDirectoryContents(subpath);
        }

        public override IChangeToken Watch(string filter)
        {
            return _provider.Watch(filter);
        }

        #endregion

        public override string Root
        {
            get => _provider.Root;
        }

        public bool UseActivePolling
        {
            get => _provider.UseActivePolling;
            set => _provider.UseActivePolling = value;
        }

        public bool UsePollingFileWatcher
        {
            get => _provider.UsePollingFileWatcher;
            set => _provider.UsePollingFileWatcher = value;
        }

        public override string MapPath(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            return fullPath == null
                ? null
                : Path.GetFullPath(fullPath);
        }

        public override bool FileExists(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            return File.Exists(fullPath);
        }

        public override bool DirectoryExists(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            return Directory.Exists(fullPath);
        }

        public override IFile GetFile(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            return fullPath.HasValue()
                ? new LocalFile(subpath, new FileInfo(fullPath), this)
                : new NotFoundFile(subpath, this);
        }

        public override IDirectory GetDirectory(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            return fullPath.HasValue()
                ? new LocalDirectory(subpath, new DirectoryInfo(fullPath), this)
                : new NotFoundDirectory(subpath, this);
        }

        public override IEnumerable<IFileEntry> EnumerateEntries(string subpath = null, string pattern = "*", bool deep = false)
        {
            var directoryInfo = new DirectoryInfo(MapPathInternal(ref subpath, true));
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{subpath}' does not exist.");
            }

            return directoryInfo
                .EnumerateFileSystemInfos(pattern, deep ? _deepEnumerationOptions : _flatEnumerationOptions)
                .Select(x =>
                {
                    if (x is FileInfo fi)
                    {
                        return ConvertFileInfo(fi);
                    }
                    else if (x is DirectoryInfo di)
                    {
                        return ConvertDirectoryInfo(di);
                    }

                    return (IFileEntry)null;
                })
                .Where(x => x != null);
        }

        public override IEnumerable<IDirectory> EnumerateDirectories(string subpath = null, string pattern = "*", bool deep = false)
        {
            var directoryInfo = new DirectoryInfo(MapPathInternal(ref subpath, true));
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{subpath}' does not exist.");
            }

            return directoryInfo
                .EnumerateDirectories(pattern, deep ? _deepEnumerationOptions : _flatEnumerationOptions)
                .Select(ConvertDirectoryInfo);
        }

        public override IEnumerable<IFile> EnumerateFiles(string subpath = null, string pattern = "*", bool deep = false)
        {
            var directoryInfo = new DirectoryInfo(MapPathInternal(ref subpath, true));
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{subpath}' does not exist.");
            }

            return directoryInfo
                .EnumerateFiles(pattern, deep ? _deepEnumerationOptions : _flatEnumerationOptions)
                .Select(ConvertFileInfo);
        }

        public override bool TryCreateDirectory(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            if (fullPath.IsEmpty())
            {
                return false;
            }

            if (File.Exists(fullPath))
            {
                throw new FileSystemException($"Cannot create directory because the path '{subpath}' already exists and is a file.");
            }

            if (Directory.Exists(fullPath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(fullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool TryDeleteDirectory(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false);
            if (fullPath.IsEmpty())
            {
                return false;
            }

            if (!Directory.Exists(fullPath))
            {
                return false;
            }

            try
            {
                Directory.Delete(fullPath, recursive: true);

                // Wait for deletion to complete
                var attempts = 0;
                while (Directory.Exists(fullPath))
                {
                    attempts += 1;
                    if (attempts > 10) return true;
                    Thread.Sleep(100);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #region IDisposable

        protected override void OnDispose(bool disposing)
        {
            _provider.Dispose();
        }

        #endregion

        #region Utils

        internal string MapPathInternal(ref string subpath, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(subpath))
                return Root;

            subpath = NormalizePath(subpath);

            var mappedPath = Path.Combine(Root, subpath);

            // Verify that the resulting path is inside the root file system path.
            if (!IsUnderneathRoot(mappedPath))
            {
                if (throwOnFailure)
                {
                    throw new FileSystemException($"The path '{subpath}' resolves to a physical path outside the file system store root.");
                }
                else
                {
                    return null;
                }
            }

            return mappedPath;
        }

        private static bool IsExcluded(FileSystemInfo fileSystemInfo, ExclusionFilters filters) => filters != ExclusionFilters.None && (fileSystemInfo.Name.StartsWith(".", StringComparison.Ordinal) && (filters & ExclusionFilters.DotPrefixed) != ExclusionFilters.None || fileSystemInfo.Exists && ((fileSystemInfo.Attributes & FileAttributes.Hidden) != (FileAttributes)0 && (filters & ExclusionFilters.Hidden) != ExclusionFilters.None || (fileSystemInfo.Attributes & FileAttributes.System) != (FileAttributes)0 && (filters & ExclusionFilters.System) != ExclusionFilters.None));

        private bool IsUnderneathRoot(string fullPath)
        {
            return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
        }

        private IDirectory ConvertDirectoryInfo(DirectoryInfo info)
        {
            var subpath = info.FullName[Root.Length..];
            return new LocalDirectory(subpath, info, this);
        }

        private IFile ConvertFileInfo(FileInfo info)
        {
            var subpath = info.FullName[Root.Length..];
            return new LocalFile(subpath, info, this);
        }

        internal void WaitForUnlockAndExecute(FileInfo fi, Action<FileInfo> action)
        {
            try
            {
                action(fi);
            }
            catch (IOException)
            {
                if (!fi.WaitForUnlock(250))
                {
                    throw;
                }

                action(fi);
            }
        }

        #endregion
    }
}