using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IO
{
    /// <summary>
    /// An implementation of <see cref="IFileSystem"/> where async methods call their sync counterparts.
    /// </summary>
    public abstract class FileSystemBase : Disposable, IFileSystem
    {
        public abstract IDirectoryContents GetDirectoryContents(string subpath);
        public abstract IFileInfo GetFileInfo(string subpath);
        public abstract IChangeToken Watch(string filter);

        /// <inheritdoc/>
        public virtual bool IsCloudStorage { get; }

        /// <inheritdoc/>
        public abstract string Root { get; }

        /// <inheritdoc/>
        public virtual string PathCombine(params string[] paths)
        {
            if (paths.Length == 0)
                return null;

            var normalizedParts =
                paths
                    .Select(x => NormalizePath(x))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray();

            var combined = string.Join("/", normalizedParts);

            // Preserve the initial '/' if it's present.
            if (paths[0]?.StartsWith('/') == true)
                combined = "/" + combined;

            return combined;
        }

        /// <inheritdoc/>
        public abstract string MapPath(string subpath);

        /// <inheritdoc/>
        public abstract bool FileExists(string subpath);

        /// <inheritdoc/>
        public virtual Task<bool> FileExistsAsync(string subpath)
            => Task.FromResult(FileExists(subpath));

        /// <inheritdoc/>
        public abstract bool DirectoryExists(string subpath);

        /// <inheritdoc/>
        public virtual Task<bool> DirectoryExistsAsync(string subpath)
            => Task.FromResult(DirectoryExists(subpath));

        /// <inheritdoc/>
        public abstract IFile GetFile(string subpath);

        /// <inheritdoc/>
        public virtual Task<IFile> GetFileAsync(string subpath)
            => Task.FromResult(GetFile(subpath));

        /// <inheritdoc/>
        public abstract IDirectory GetDirectory(string subpath);

        /// <inheritdoc/>
        public virtual Task<IDirectory> GetDirectoryAsync(string subpath)
            => Task.FromResult(GetDirectory(subpath));

        /// <inheritdoc/>
        public abstract IEnumerable<IFileEntry> EnumerateEntries(string subpath = null, string pattern = "*", bool deep = false);

        /// <inheritdoc/>
        public virtual Task<IEnumerable<IFileEntry>> EnumerateEntriesAsync(string subpath = null, string pattern = "*", bool deep = false)
            => Task.FromResult(EnumerateEntries(subpath, pattern, deep));

        /// <inheritdoc/>
        public abstract long GetDirectorySize(string subpath, string pattern, Func<string, bool> predicate, bool deep = true);

        /// <inheritdoc/>
        public virtual Task<long> GetDirectorySizeAsync(string subpath, string pattern, Func<string, bool> predicate, bool deep = true)
            => Task.FromResult(GetDirectorySize(subpath, pattern, predicate, deep));

        /// <inheritdoc/>
        public abstract long CountFiles(string subpath, string pattern, Func<string, bool> predicate, bool deep = true);

        /// <inheritdoc/>
        public virtual Task<long> CountFilesAsync(string subpath, string pattern, Func<string, bool> predicate, bool deep = true)
            => Task.FromResult(CountFiles(subpath, pattern, predicate, deep));

        /// <inheritdoc/>
        public abstract bool TryCreateDirectory(string subpath);

        /// <inheritdoc/>
        public virtual Task<bool> TryCreateDirectoryAsync(string subpath)
            => Task.FromResult(TryCreateDirectory(subpath));

        /// <inheritdoc/>
        public abstract bool TryDeleteDirectory(string subpath);

        /// <inheritdoc/>
        public virtual Task<bool> TryDeleteDirectoryAsync(string subpath)
            => Task.FromResult(TryDeleteDirectory(subpath));

        /// <inheritdoc/>
        public abstract void MoveEntry(IFileEntry entry, string newPath);

        /// <inheritdoc/>
        public virtual Task MoveEntryAsync(IFileEntry entry, string newPath)
        {
            MoveEntry(entry, newPath);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public abstract bool CheckUniqueFileName(string subpath, out string newPath);

        /// <inheritdoc/>
        public virtual Task<bool> CheckUniqueFileNameAsync(string subpath, Action<string> success)
        {
            if (CheckUniqueFileName(subpath, out var newPath))
            {
                success?.Invoke(newPath);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public abstract bool TryDeleteFile(string subpath);

        /// <inheritdoc/>
        public virtual Task<bool> TryDeleteFileAsync(string subpath)
            => Task.FromResult(TryDeleteFile(subpath));

        /// <inheritdoc/>
        public abstract IFile CreateFile(string subpath, Stream inStream = null, bool overwrite = false);

        /// <inheritdoc/>
        public virtual Task<IFile> CreateFileAsync(string subpath, Stream inStream = null, bool overwrite = false)
            => Task.FromResult(CreateFile(subpath, inStream, overwrite));

        /// <inheritdoc/>
        public abstract void CopyFile(string subpath, string newPath, bool overwrite = false);

        /// <inheritdoc/>
        public virtual Task CopyFileAsync(string subpath, string newPath, bool overwrite = false)
        {
            CopyFile(subpath, newPath, overwrite);
            return Task.CompletedTask;
        }

        #region Protected utils

        /// <summary>
        /// Normalizes a path using the path delimiter semantics of the abstract virtual file system.
        /// </summary>
        /// <remarks>
        /// Backslash is converted to forward slash and any leading or trailing slashes are removed.
        /// </remarks>
        protected internal static string NormalizePath(string path)
        {
            return path?.Replace('\\', '/').Trim('/');
        }

        #endregion
    }
}
