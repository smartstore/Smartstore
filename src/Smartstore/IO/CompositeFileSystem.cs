using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Composite;
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
        private readonly IFileSystem _primary;
        private readonly string _root;

        public CompositeFileSystem(params IFileSystem[] fileSystems)
            : this(fileSystems.AsEnumerable())
        {
        }

        public CompositeFileSystem(IEnumerable<IFileSystem> fileSystems)
        {
            Guard.NotNull(fileSystems);
            
            if (!fileSystems.Any())
            {
                throw new ArgumentException("CompositeFileSystem must contain at least one item.", nameof(fileSystems));
            }

            _fileSystems = fileSystems.ToArray();
            _root = _fileSystems[0].Root;

            if (_fileSystems.Length == 1)
            {
                // Perf
                _primary = _fileSystems[0];
            }
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
            if (_primary != null) 
            {
                return checker(_primary, subpath);
            }
            
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
            if (_primary != null)
            {
                return await checker(_primary, subpath);
            }

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
            if (_primary != null)
            {
                return getter(_primary, subpath);
            }

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
            if (_primary != null)
            {
                return await getter(_primary, subpath);
            }

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
            => Get(subpath, (fs, path) => fs.GetFile(path));

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (_primary != null)
            {
                return _primary.GetDirectoryContents(subpath);
            }
            else
            {
                return new CompositeDirectoryContents(_fileSystems, subpath);
            }
        }

        public virtual IChangeToken Watch(string pattern)
        {
            if (_primary != null)
            {
                return _primary.Watch(pattern);
            }

            // Watch all file providers
            var changeTokens = new List<IChangeToken>();
            foreach (IFileProvider fileProvider in _fileSystems)
            {
                IChangeToken changeToken = fileProvider.Watch(pattern);
                if (changeToken != null)
                {
                    changeTokens.Add(changeToken);
                }
            }

            // There is no change token with active change callbacks
            if (changeTokens.Count == 0)
            {
                return NullChangeToken.Singleton;
            }

            return new CompositeChangeToken(changeTokens);
        }

        public string MapPath(string subpath)
        {
            if (_primary != null)
            {
                return _primary.MapPath(subpath);
            }

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
