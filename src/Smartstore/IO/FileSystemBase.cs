using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
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
            var dir = GetDirectory(file.Directory);
            var files = new HashSet<string>(dir.EnumerateFiles(pattern, false).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

            int i = 1;
            while (true)
            {
                var fileName = string.Concat(file.NameWithoutExtension, "-", i, file.Extension);
                if (!files.Contains(fileName))
                {
                    // Found our gap
                    newPath = PathCombine(dir.SubPath, fileName);
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
            var dir = await GetDirectoryAsync(file.Directory);
            var names = await dir.EnumerateFilesAsync(pattern, false).Select(x => x.Name).AsyncToArray();
            var files = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            int i = 1;
            while (true)
            {
                var fileName = string.Concat(file.NameWithoutExtension, "-", i, file.Extension);
                if (!files.Contains(fileName))
                {
                    // Found our gap
                    return new AsyncOut<string>(true, PathCombine(dir.SubPath, fileName));
                }

                i++;
            }
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
