using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Threading;

namespace Smartstore.IO
{
    /// <summary>
    /// Looks up files using a collection of <see cref="IFileSystem"/>.
    /// </summary>
    public class CompositeFileSystem : IFileSystem
    {
        private readonly IFileSystem[] _fileSystems;
        private readonly CompositeFileProvider _compositeFileProvider;
        private readonly string _root;

        public CompositeFileSystem(params IFileSystem[] fileSystems)
            : this(fileSystems.AsEnumerable())
        {
        }

        public CompositeFileSystem(IEnumerable<IFileSystem> fileSystems)
        {
            Guard.NotNull(fileSystems, nameof(fileSystems));
            if (!fileSystems.Any())
            {
                throw new ArgumentException("CompositeFileSystem must contain at least one item.", nameof(fileSystems));
            }

            _fileSystems = fileSystems.ToArray();
            _compositeFileProvider = new CompositeFileProvider(_fileSystems);
            _root = _fileSystems[0].Root;
        }

        /// <summary>
        /// Gets the list of configured <see cref="IFileSystem" /> instances.
        /// </summary>
        public IEnumerable<IFileSystem> FileSystems => _fileSystems;

        string IFileSystem.Root
        {
            get => _root;
        }

        private bool Check(string subpath, Func<IFileSystem, string, bool> checker)
        {
            foreach (var fs in _fileSystems)
            {
                if (checker(fs, subpath))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> CheckAsync(string subpath, Func<IFileSystem, string, Task<bool>> checker)
        {
            foreach (var fs in _fileSystems)
            {
                if (await checker(fs, subpath))
                {
                    return true;
                }
            }

            return false;
        }

        private T Get<T>(string subpath, Func<IFileSystem, string, T> getter)
            where T : IFileEntry
        {
            T entry = default;
            foreach (var fs in _fileSystems)
            {
                entry = getter(fs, subpath);
                if (entry != null && entry.Exists)
                {
                    return entry;
                }
            }

            return entry;
        }

        private async Task<T> GetAsync<T>(string subpath, Func<IFileSystem, string, Task<T>> getter)
            where T : IFileEntry
        {
            T entry = default;
            foreach (var fs in _fileSystems)
            {
                entry = await getter(fs, subpath);
                if (entry != null && entry.Exists)
                {
                    return entry;
                }
            }

            return entry;
        }

        public bool DirectoryExists(string subpath)
            => Check(subpath, (fs, path) => fs.DirectoryExists(path));
        public Task<bool> DirectoryExistsAsync(string subpath)
            => CheckAsync(subpath, (fs, path) => fs.DirectoryExistsAsync(path));
        public bool FileExists(string subpath)
            => Check(subpath, (fs, path) => fs.FileExists(path));
        public Task<bool> FileExistsAsync(string subpath)
            => CheckAsync(subpath, (fs, path) => fs.FileExistsAsync(path));

        public IDirectory GetDirectory(string subpath)
            => Get(subpath, (fs, path) => fs.GetDirectory(path));
        public Task<IDirectory> GetDirectoryAsync(string subpath)
            => GetAsync(subpath, (fs, path) => fs.GetDirectoryAsync(path));
        public IFile GetFile(string subpath)
            => Get(subpath, (fs, path) => fs.GetFile(path));
        public Task<IFile> GetFileAsync(string subpath)
            => GetAsync(subpath, (fs, path) => fs.GetFileAsync(path));

        public IFileInfo GetFileInfo(string subpath)
            => _compositeFileProvider.GetFileInfo(subpath);
        public IDirectoryContents GetDirectoryContents(string subpath)
            => _compositeFileProvider.GetDirectoryContents(subpath);
        public IChangeToken Watch(string filter)
            => _compositeFileProvider.Watch(filter);

        public string MapPath(string subpath)
        {
            IFileEntry entry = null;
            foreach (var fs in _fileSystems)
            {
                entry = fs.GetEntry(subpath);
                if (entry != null && entry.Exists)
                {
                    break;
                }
            }

            return entry?.PhysicalPath;
        }

        public bool CheckUniqueFileName(string subpath, out string newPath)
            => throw new NotSupportedException();
        public Task<AsyncOut<string>> CheckUniqueFileNameAsync(string subpath)
            => throw new NotSupportedException();
    }
}
