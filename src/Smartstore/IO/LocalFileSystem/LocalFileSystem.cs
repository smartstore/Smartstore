using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IO
{
    /// <summary>
    /// Looks up files using the local disk file system
    /// </summary>
    /// <remarks>
    /// When the environment variable "DOTNET_USE_POLLING_FILE_WATCHER" is set to "1" or "true", calls to
    /// <see cref="Watch(string)" /> will use <see cref="PollingFileChangeToken" />.
    /// </remarks>
    [DebuggerDisplay("LocalFileSystem - Root: {Root}, UseActivePolling: {UseActivePolling}, UsePollingFileWatcher: {UsePollingFileWatcher}")]
    public class LocalFileSystem : FileSystemBase, IFileProvider
    {
        private readonly PhysicalFileProvider _provider;
        private readonly ExclusionFilters _filters;

        /// <summary>
        /// Initializes a new instance of a LocalFileSystem with the given physical root path.
        /// </summary>
        public LocalFileSystem(string root)
        {
            Guard.NotEmpty(root);

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
            return MapPathInternal(ref subpath, false, false);
        }

        public override bool FileExists(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            var file = new FileInfo(fullPath);
            return !IsExcluded(file) && file.Exists;
        }

        public override bool DirectoryExists(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, false, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            var dir = new DirectoryInfo(fullPath);
            return !IsExcluded(dir) && dir.Exists;
        }

        public override IFile GetFile(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, true, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return new NotFoundFile(subpath, this);
            }

            var file = new FileInfo(fullPath);

            return !IsExcluded(file)
                ? new LocalFile(subpath, file, this)
                : new NotFoundFile(subpath, this);
        }

        public override IDirectory GetDirectory(string subpath)
        {
            var fullPath = MapPathInternal(ref subpath, true, false);
            if (string.IsNullOrEmpty(fullPath))
            {
                return new NotFoundDirectory(subpath, this);
            }

            var dir = new DirectoryInfo(fullPath);

            return !IsExcluded(dir)
                ? new LocalDirectory(subpath, new DirectoryInfo(fullPath), this)
                : new NotFoundDirectory(subpath, this);
        }

        #region IDisposable

        protected override void OnDispose(bool disposing)
        {
            _provider.Dispose();
        }

        #endregion      

        #region Utils

        internal string MapPathInternal(ref string subpath, bool transformSeparators, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                subpath ??= string.Empty;
                return Root;
            }

            var len = subpath.Length;
            var span = subpath.AsSpan()
                .Trim()
                .TrimStart('~')
                .TrimStart(PathUtility.PathSeparators);

            transformSeparators = transformSeparators && span.IndexOf('\\') != -1;

            if (transformSeparators)
            {
                span = ApplySeparatorTransform(span);
            }

            if (transformSeparators || span.Length != len)
            {
                subpath = span.ToString();
            }

            var mappedPath = Path.Join(Root, subpath);

            //// TODO: (core) Do we really really need this check?
            //// Verify that the resulting path is inside the root file system path.
            //if (!IsUnderneathRoot(mappedPath))
            //{
            //    if (throwOnFailure)
            //    {
            //        throw new FileSystemException($"The path '{subpath}' resolves to a physical path outside the file system store root.");
            //    }
            //    else
            //    {
            //        return null;
            //    }
            //}

            return Path.GetFullPath(mappedPath);

            static ReadOnlySpan<char> ApplySeparatorTransform(ReadOnlySpan<char> value)
            {
                var destination = new char[value.Length];

                for (var i = 0; i < value.Length; i++)
                {
                    destination[i] = value[i] == '\\' ? '/' : value[i];
                }

                return destination;
            }
        }

        internal static bool IsExcluded(FileSystemInfo info)
        {
            // Sync filtering behaviour with PhysicalFileProvider

            if (info.Name.StartsWith('.'))
            {
                return true;
            }
            else if (info.Exists && ((info.Attributes & FileAttributes.Hidden) != 0 || ((info.Attributes & FileAttributes.System) != 0)))
            {
                return true;
            }

            return false;
        }

        //private bool IsUnderneathRoot(string fullPath)
        //{
        //    return fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);
        //}

        #endregion
    }
}