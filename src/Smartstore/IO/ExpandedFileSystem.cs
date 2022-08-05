using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Threading;

namespace Smartstore.IO
{
    /// <summary>
    /// A FileSystem decorator that takes a path prefix and an inner FileSystem instance
    /// whose root path should be expanded with a prefix.
    /// </summary>
    public class ExpandedFileSystem : IFileSystem
    {
        private readonly PathString _pathPrefix;

        public ExpandedFileSystem(string pathPrefix, IFileSystem inner)
        {
            Guard.NotEmpty(pathPrefix, nameof(pathPrefix));
            Guard.NotNull(inner, nameof(inner));

            // No leading or trailing slash
            PathPrefix = PathUtility.NormalizeRelativePath(pathPrefix).TrimEnd('/');
            _pathPrefix = new PathString('/' + PathPrefix);

            InnerProvider = inner;
            Root = Path.GetFullPath(Path.Combine(inner.Root, PathPrefix)).EnsureEndsWith(Path.DirectorySeparatorChar);
        }

        public string PathPrefix { get; }
        public IFileSystem InnerProvider { get; }
        public string Root { get; }

        protected string Expand(string path)
        {
            if (path.IsEmpty() || path == PathPrefix)
            {
                return PathPrefix;
            }

            if (path.Length > PathPrefix.Length)
            {
                if (((PathString)path.EnsureStartsWith('/')).StartsWithSegments(_pathPrefix, StringComparison.CurrentCulture))
                {
                    return path;
                }
            }

            return PathPrefix + path.EnsureStartsWith('/');
        }

        private string FixPath(string path)
        {
            if (path.Length > PathPrefix.Length)
            {
                if (((PathString)path.EnsureStartsWith('/')).StartsWithSegments(_pathPrefix, StringComparison.CurrentCulture, out var remaining))
                {
                    return remaining.Value.TrimStart('/');
                }
            }

            return path;
        }

        public bool DirectoryExists(string subpath)
            => InnerProvider.DirectoryExists(Expand(subpath));
        public Task<bool> DirectoryExistsAsync(string subpath)
            => InnerProvider.DirectoryExistsAsync(Expand(subpath));
        public bool FileExists(string subpath)
            => InnerProvider.FileExists(Expand(subpath));
        public Task<bool> FileExistsAsync(string subpath)
            => InnerProvider.FileExistsAsync(Expand(subpath));

        public IDirectory GetDirectory(string subpath)
            => InnerProvider.GetDirectory(Expand(subpath));
        public Task<IDirectory> GetDirectoryAsync(string subpath)
            => InnerProvider.GetDirectoryAsync(Expand(subpath));
        public IDirectoryContents GetDirectoryContents(string subpath)
            => InnerProvider.GetDirectoryContents(Expand(subpath));
        public IFile GetFile(string subpath)
            => InnerProvider.GetFile(Expand(subpath));
        public Task<IFile> GetFileAsync(string subpath)
            => InnerProvider.GetFileAsync(Expand(subpath));
        public IFileInfo GetFileInfo(string subpath)
            => InnerProvider.GetFileInfo(Expand(subpath));
        public IChangeToken Watch(string filter)
            => InnerProvider.Watch(Expand(filter));

        public string MapPath(string subpath)
            => InnerProvider.MapPath(Expand(subpath));

        public bool CheckUniqueFileName(string subpath, out string newPath)
        {
            if (InnerProvider.CheckUniqueFileName(Expand(subpath), out newPath))
            {
                newPath = FixPath(newPath);
                return true;
            }

            return false;
        }
        public async Task<AsyncOut<string>> CheckUniqueFileNameAsync(string subpath)
        {
            var result = await InnerProvider.CheckUniqueFileNameAsync(Expand(subpath));
            if (result.Success)
            {
                return new AsyncOut<string>(true, FixPath(result.Value));
            }

            return result;
        }
    }
}
