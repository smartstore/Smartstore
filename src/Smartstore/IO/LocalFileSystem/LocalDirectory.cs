using System.Diagnostics;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.IO
{
    [DebuggerDisplay("LocalDirectory: {SubPath}")]
    public class LocalDirectory : IDirectory
    {
        // Defaults are: AttributesToSkip = FileAttributes.Hidden | FileAttributes.System, IgnoreInaccessible = true
        internal static readonly EnumerationOptions FlatEnumerationOptions = new();
        internal static readonly EnumerationOptions DeepEnumerationOptions = new() { RecurseSubdirectories = true };

        private readonly DirectoryInfo _di;
        private LocalFileSystem _fs;

        public LocalDirectory(string subpath, DirectoryInfo info, LocalFileSystem fileSystem)
        {
            _di = info;
            _fs = fileSystem;

            SubPath = PathUtility.NormalizeRelativePath(subpath);
        }

        public DirectoryInfo AsDirectoryInfo() => _di;

        /// <inheritdoc/>
        public IFileSystem FileSystem
        {
            get => _fs;
            protected internal set => _fs = Guard.NotNull(value as LocalFileSystem, nameof(value));
        }

        /// <inheritdoc/>
        public string SubPath { get; }

        /// <inheritdoc />
        public bool Exists
        {
            get => _di.Exists;
        }

        /// <inheritdoc />
        public bool IsRoot
        {
            get => SubPath.IsEmpty() || SubPath == "/";
        }

        /// <inheritdoc />
        public long Length { get; } = -1;

        /// <inheritdoc />
        public string PhysicalPath
        {
            get => _di.FullName;
        }

        /// <inheritdoc />
        public string Name
        {
            get => _di.Name;
        }

        /// <inheritdoc />
        public DateTimeOffset CreatedOn
        {
            get => _di.CreationTimeUtc;
        }

        /// <inheritdoc />
        public DateTimeOffset LastModified
        {
            get => _di.LastWriteTimeUtc;
        }

        /// <summary>
        /// Always true.
        /// </summary>
        public bool IsDirectory
        {
            get => true;
        }

        public bool IsSymbolicLink(out string finalTargetPath)
        {
            var linkTarget = _di.ResolveLinkTarget(true);
            finalTargetPath = linkTarget?.FullName;
            return linkTarget != null;
        }

        public IDirectory Parent
        {
            get
            {
                if (!IsRoot && _di.Parent != null)
                {
                    return new LocalDirectory(Path.GetDirectoryName(SubPath.TrimEnd('/')), _di.Parent, _fs);
                }

                return null;
            }
        }

        Stream IFileInfo.CreateReadStream()
            => throw new NotSupportedException();

        public void Delete()
        {
            _di.Delete(true);
        }

        public void Create()
        {
            _di.Create();
        }

        public IDirectory CreateSubdirectory(string path)
        {
            path = PathUtility.NormalizeRelativePath(path);
            var di = _di.CreateSubdirectory(path);
            return new LocalDirectory(PathUtility.Join(SubPath, path), di, _fs);
        }

        public void MoveTo(string newPath)
        {
            Guard.NotNull(newPath);

            if (!_di.Exists)
            {
                throw new DirectoryNotFoundException($"Cannot move directory '{SubPath}' because it does not exist.");
            }

            var fullDstPath = _fs.MapPathInternal(ref newPath, true, true);

            if (Directory.Exists(fullDstPath))
            {
                throw new FileSystemException($"Cannot move directory because the target path '{newPath}' already exists.");
            }

            _di.MoveTo(fullDstPath);
        }

        public IEnumerable<IFileEntry> EnumerateEntries(string pattern = "*", bool deep = false)
        {
            if (!_di.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{SubPath}' does not exist.");
            }
            
            return _di
                .EnumerateFileSystemInfos(pattern, deep ? DeepEnumerationOptions : FlatEnumerationOptions)
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

        public IEnumerable<IDirectory> EnumerateDirectories(string pattern = "*", bool deep = false)
        {
            if (!_di.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{SubPath}' does not exist.");
            }

            return _di
                .EnumerateDirectories(pattern, deep ? DeepEnumerationOptions : FlatEnumerationOptions)
                .Select(ConvertDirectoryInfo);
        }

        public IEnumerable<IFile> EnumerateFiles(string pattern = "*", bool deep = false)
        {
            if (!_di.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{SubPath}' does not exist.");
            }

            return _di
                .EnumerateFiles(pattern, deep ? DeepEnumerationOptions : FlatEnumerationOptions)
                .Select(ConvertFileInfo);
        }

        private IDirectory ConvertDirectoryInfo(DirectoryInfo info)
        {
            var subpath = info.FullName[_fs.Root.Length..];
            return new LocalDirectory(subpath, info, _fs);
        }

        private IFile ConvertFileInfo(FileInfo info)
        {
            var subpath = info.FullName[_fs.Root.Length..];
            return new LocalFile(subpath, info, _fs);
        }
    }
}