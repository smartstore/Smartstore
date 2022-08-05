using System.Runtime.CompilerServices;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckUniqueFileName(string subpath, out string newPath)
        {
            if (CheckUniqueFileNameCore(subpath, false).Await().Out(out newPath))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<AsyncOut<string>> CheckUniqueFileNameAsync(string subpath)
            => CheckUniqueFileNameCore(subpath, true);

        protected internal virtual async Task<AsyncOut<string>> CheckUniqueFileNameCore(string subpath, bool async)
        {
            Guard.NotEmpty(subpath, nameof(subpath));

            var file = async ? await GetFileAsync(subpath) : GetFile(subpath);
            if (!file.Exists)
            {
                return AsyncOut<string>.Empty;
            }

            var pattern = file.NameWithoutExtension + "-*" + file.Extension;
            var dir = async
                ? await GetDirectoryAsync(file.Directory)
                : GetDirectory(file.Directory);
            var names = async
                ? await dir.EnumerateFilesAsync(pattern, false).Select(x => x.Name).AsyncToArray()
                : dir.EnumerateFiles(pattern, false).Select(x => x.Name);

            var files = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            int i = 1;
            while (true)
            {
                var fileName = string.Concat(file.NameWithoutExtension, "-", i, file.Extension);
                if (!files.Contains(fileName))
                {
                    // Found our gap
                    return new AsyncOut<string>(true, PathUtility.Join(dir.SubPath, fileName));
                }

                i++;
            }
        }
    }
}
