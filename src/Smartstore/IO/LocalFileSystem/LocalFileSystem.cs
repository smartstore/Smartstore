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
using Smartstore.Net;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string MapPath(string subpath)
        {
            return MapPathInternal(subpath, false);
        }

        public override bool FileExists(string subpath)
        {
            var fullPath = MapPathInternal(subpath, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            return File.Exists(fullPath);
        }

        public override bool DirectoryExists(string subpath)
        {
            var fullPath = MapPathInternal(subpath, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            return Directory.Exists(fullPath);
        }

        public override IFile GetFile(string subpath)
        {
            var fullPath = MapPathInternal(subpath, false);
            return fullPath.HasValue()
                ? new LocalFile(subpath, new FileInfo(fullPath), this)
                : new NotFoundFile(subpath, this);
        }

        public override IDirectory GetDirectory(string subpath)
        {
            var fullPath = MapPathInternal(subpath, false);
            return fullPath.HasValue()
                ? new LocalDirectory(subpath, new DirectoryInfo(fullPath), this)
                : new NotFoundDirectory(subpath, this);
        }

        /// <inheritdoc/>
        public override long GetDirectorySize(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return EnumerateFiles(subpath, pattern, deep)
                .AsParallel()
                .Where(x => predicate == null || predicate(x.SubPath))
                .Sum(x => x.Length);
        }

        public override long CountFiles(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return EnumerateFiles(subpath, pattern, deep)
                .AsParallel()
                .Where(x => predicate == null || predicate(x.SubPath))
                .Count();
        }

        public override IEnumerable<IFileEntry> EnumerateEntries(string subpath = null, string pattern = "*", bool deep = false)
        {
            var directoryInfo = new DirectoryInfo(MapPathInternal(subpath, true));
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
            var directoryInfo = new DirectoryInfo(MapPathInternal(subpath, true));
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
            var directoryInfo = new DirectoryInfo(MapPathInternal(subpath, true));
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
            var fullPath = MapPathInternal(subpath, false);
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
            var fullPath = MapPathInternal(subpath, false);
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

        public override void MoveEntry(IFileEntry entry, string newPath)
        {
            Guard.NotNull(entry, nameof(entry));

            if (!entry.Exists)
            {
                throw new FileSystemException($"Cannot move file system entry '{entry.SubPath}' because it does not exist.");
            }

            var fullTargetPath = MapPathInternal(newPath, true);

            if (entry.IsDirectory)
            {
                if (Directory.Exists(fullTargetPath))
                {
                    throw new FileSystemException($"Cannot move directory because the target path '{newPath}' already exists.");
                }

                Directory.Move(entry.PhysicalPath, fullTargetPath);
            }
            else if (entry is LocalFile localFile)
            {
                if (File.Exists(fullTargetPath))
                {
                    throw new FileSystemException($"Cannot move file because the target path '{newPath}' already exists.");
                }

                if (localFile.AsFileInfo() != null)
                {
                    WaitForUnlockAndExecute(localFile.AsFileInfo(), x => File.Move(localFile.PhysicalPath, fullTargetPath));
                }
                else
                {
                    File.Move(localFile.PhysicalPath, fullTargetPath);
                }
            }
        }

        public override bool CheckUniqueFileName(string subpath, out string newPath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));

            newPath = null;

            var file = GetFile(subpath);
            if (!file.Exists)
            {
                return false;
            }

            var pattern = file.NameWithoutExtension + "-*" + file.Extension;
            var dir = file.Directory;
            var files = new HashSet<string>(EnumerateFiles(dir, pattern, false).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

            int i = 1;
            while (true)
            {
                var fileName = string.Concat(file.NameWithoutExtension, "-", i, file.Extension);
                if (!files.Contains(fileName))
                {
                    // Found our gap
                    newPath = PathCombine(dir, fileName);
                    return true;
                }

                i++;
            }
        }

        public override bool TryDeleteFile(string subpath)
        {
            var fullPath = MapPathInternal(subpath, false);
            if (fullPath.IsEmpty())
            {
                return false;
            }

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                return false;
            }

            try
            {
                WaitForUnlockAndExecute(fileInfo, x => x.Delete());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override IFile CreateFile(string subpath, Stream inStream = null, bool overwrite = false)
        {
            var fileInfo = StartCreateFile(subpath, overwrite);

            using (var outputStream = fileInfo.Create())
            {
                if (inStream != null)
                {
                    inStream.CopyTo(outputStream);
                }
            }

            return new LocalFile(subpath, fileInfo, this);
        }

        public override async Task<IFile> CreateFileAsync(string subpath, Stream inStream = null, bool overwrite = false)
        {
            var fileInfo = StartCreateFile(subpath, overwrite);

            using (var outputStream = fileInfo.Create())
            {
                if (inStream != null)
                {
                    await inStream.CopyToAsync(outputStream);
                }
            }

            return new LocalFile(subpath, fileInfo, this);
        }

        public override void CopyFile(string subpath, string newPath, bool overwrite = false)
        {
            Guard.NotNull(subpath, nameof(subpath));

            var fullSrcPath = MapPathInternal(subpath, true);
            var sourceFileInfo = new FileInfo(fullSrcPath);

            if (!sourceFileInfo.Exists)
            {
                throw new FileSystemException($"The file '{subpath}' does not exist.");
            }

            var fullDstPath = MapPathInternal(newPath, true);

            if (Directory.Exists(fullDstPath))
            {
                throw new FileSystemException($"Cannot copy file to '{newPath}' because it already exists as a directory.");
            }

            if (!overwrite && File.Exists(fullDstPath))
            {
                throw new FileSystemException($"Cannot copy file because the destination path '{newPath}' already exists.");
            }

            WaitForUnlockAndExecute(sourceFileInfo, x => File.Copy(fullSrcPath, fullDstPath, overwrite));
        }

        #region IDisposable

        protected override void OnDispose(bool disposing)
        {
            _provider.Dispose();
        }

        #endregion

        #region Utils

        private string MapPathInternal(string subpath, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(subpath))
                return Root;

            if (PathHelper.HasInvalidPathChars(subpath) || Path.IsPathRooted(subpath) || PathHelper.PathNavigatesAboveRoot(subpath))
            {
                if (throwOnFailure)
                {
                    throw new DirectoryNotFoundException($"Directory '${subpath}' does not exist.");
                }
                else
                {
                    return null;
                }
            }

            var sepChar = Path.DirectorySeparatorChar; // --> '\\'

            // Check if path ends with / or \
            var hasTrailingSlash = subpath[^1] is ('/' or '\\');

            // Convert "/myshop/file.png" --> "/file.png"
            if (WebHelper.IsAbsolutePath(subpath, out var relativePath))
            {
                subpath = relativePath.Value;
            }

            var mappedPath = Root + subpath.Trim('~').Replace('/', sepChar).Trim(sepChar);

            // Verify that the resulting path is inside the root file system path.
            if (!IsUnderneathRoot(mappedPath))
            {
                if (throwOnFailure)
                {
                    throw new DirectoryNotFoundException($"Directory '${subpath}' does not exist.");
                }
                else
                {
                    return null;
                }
            }

            if (hasTrailingSlash)
            {
                mappedPath += sepChar;
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

        private FileInfo StartCreateFile(string subpath, bool overwrite)
        {
            Guard.NotNull(subpath, nameof(subpath));

            var fullPath = MapPathInternal(subpath, true);

            if (!overwrite && File.Exists(fullPath))
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists.");
            }

            if (Directory.Exists(fullPath))
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists as a directory.");
            }

            // Create directory path if it doesn't exist.
            var dirPath = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(dirPath);

            return new FileInfo(fullPath);
        }

        private static void WaitForUnlockAndExecute(FileInfo fi, Action<FileInfo> action)
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