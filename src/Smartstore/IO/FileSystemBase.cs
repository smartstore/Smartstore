using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Dasync.Collections;
using Smartstore.Threading;

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
        public abstract string Root { get; }

        /// <inheritdoc/>
        public virtual string PathCombine(params string[] paths)
        {
            return PathUtility.Combine(paths);
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
        public virtual IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(string subpath = null, string pattern = "*", bool deep = false)
            => EnumerateEntries(subpath, pattern, deep).ToAsyncEnumerable();

        /// <inheritdoc/>
        public abstract IEnumerable<IFile> EnumerateFiles(string subpath = null, string pattern = "*", bool deep = false);

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<IFile> EnumerateFilesAsync(string subpath = null, string pattern = "*", bool deep = false)
            => EnumerateFiles(subpath, pattern, deep).ToAsyncEnumerable();

        /// <inheritdoc/>
        public abstract IEnumerable<IDirectory> EnumerateDirectories(string subpath = null, string pattern = "*", bool deep = false);

        /// <inheritdoc/>
        public virtual IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(string subpath = null, string pattern = "*", bool deep = false)
            => EnumerateDirectories(subpath, pattern, deep).ToAsyncEnumerable();

        /// <inheritdoc/>
        public virtual long GetDirectorySize(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return EnumerateFiles(subpath, pattern, deep)
                .AsParallel()
                .Where(x => predicate == null || predicate(x.SubPath))
                .Sum(x => x.Length);
        }

        /// <inheritdoc/>
        public virtual async Task<long> GetDirectorySizeAsync(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return await EnumerateFilesAsync(subpath, pattern, deep)
                .Where(x => predicate == null || predicate(x.SubPath))
                .SumAsync(x => x.Length);
        }

        /// <inheritdoc/>
        public virtual long CountFiles(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return EnumerateFiles(subpath, pattern, deep)
                .AsParallel()
                .Where(x => predicate == null || predicate(x.SubPath))
                .Count();
        }

        /// <inheritdoc/>
        public virtual async Task<long> CountFilesAsync(string subpath, string pattern = "*", Func<string, bool> predicate = null, bool deep = true)
        {
            return await EnumerateFilesAsync(subpath, pattern, deep)
                .Where(x => predicate == null || predicate(x.SubPath))
                .CountAsync();
        }

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
        public virtual bool CheckUniqueFileName(string subpath, out string newPath)
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

        /// <inheritdoc/>
        public virtual async Task<AsyncOut<string>> CheckUniqueFileNameAsync(string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));

            var file = GetFile(subpath);
            if (!file.Exists)
            {
                return AsyncOut<string>.Empty;
            }

            var pattern = file.NameWithoutExtension + "-*" + file.Extension;
            var dir = file.Directory;
            var names = await EnumerateFilesAsync(dir, pattern, false).Select(x => x.Name).AsyncToArray();
            var files = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            int i = 1;
            while (true)
            {
                var fileName = string.Concat(file.NameWithoutExtension, "-", i, file.Extension);
                if (!files.Contains(fileName))
                {
                    // Found our gap
                    return new AsyncOut<string>(true, PathCombine(dir, fileName));
                }

                i++;
            }
        }

        /// <inheritdoc/>
        public virtual bool TryDeleteFile(string subpath)
        {
            var file = GetFile(subpath);

            if (file.PhysicalPath.IsEmpty())
            {
                return false;
            }

            if (!file.Exists)
            {
                return false;
            }

            try
            {
                file.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> TryDeleteFileAsync(string subpath)
        {
            var file = await GetFileAsync(subpath);

            if (file.PhysicalPath.IsEmpty())
            {
                return false;
            }

            if (!file.Exists)
            {
                return false;
            }

            try
            {
                await file.DeleteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual IFile CreateFile(string subpath, Stream inStream = null, bool overwrite = false)
        {
            if (DirectoryExists(subpath))
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists as a directory.");
            }

            var file = GetFile(subpath);
            file.Create(inStream, overwrite);
            return file;
        }

        /// <inheritdoc/>
        public virtual async Task<IFile> CreateFileAsync(string subpath, Stream inStream = null, bool overwrite = false)
        {
            if (await DirectoryExistsAsync(subpath))
            {
                throw new FileSystemException($"Cannot create file '{subpath}' because it already exists as a directory.");
            }

            var file = await GetFileAsync(subpath);
            await file.CreateAsync(inStream, overwrite);
            return file;
        }

        /// <inheritdoc/>
        public virtual void CopyFile(string subpath, string newPath, bool overwrite = false)
        {
            var file = GetFile(subpath);
            file.CopyTo(newPath, overwrite);
        }

        /// <inheritdoc/>
        public virtual async Task CopyFileAsync(string subpath, string newPath, bool overwrite = false)
        {
            var file = await GetFileAsync(subpath);
            await file.CopyToAsync(newPath, overwrite);
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
            return path?.Trim('~').Replace('\\', '/').Trim('/', ' ');
        }

        #endregion
    }
}
