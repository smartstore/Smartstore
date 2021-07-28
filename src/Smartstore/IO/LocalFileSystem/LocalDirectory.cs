using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO.SymLinks;

namespace Smartstore.IO
{
    [DebuggerDisplay("LocalDirectory: {SubPath}")]
    public class LocalDirectory : IDirectory
    {
        private readonly DirectoryInfo _di;
        private LocalFileSystem _fs;

        public LocalDirectory(string subpath, DirectoryInfo info, LocalFileSystem fileSystem)
        {
            _di = info;
            _fs = fileSystem;

            SubPath = FileSystemBase.NormalizePath(subpath);
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
        public DateTimeOffset LastModified
        {
            get => _di.LastWriteTimeUtc;
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsDirectory
        {
            get => true;
        }

        public bool IsSymbolicLink(out string finalPhysicalPath)
            => _di.IsSymbolicLink(out finalPhysicalPath);

        public IDirectory Parent
        {
            get
            {
                if (!IsRoot && _di.Parent != null)
                {
                    return new LocalDirectory(Path.GetDirectoryName(SubPath), _di.Parent, _fs);
                }

                return null;
            }
        }

        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();

        public void Delete()
        {
            _di.Delete(true);

            // Wait for deletion to complete
            var attempts = 0;
            while (_di.Exists)
            {
                attempts += 1;
                if (attempts > 10) return;
                Thread.Sleep(100);
            }
        }

        public void MoveTo(string newPath)
        {
            Guard.NotNull(newPath, nameof(newPath));

            if (!_di.Exists)
            {
                throw new FileSystemException($"Cannot move directory '{SubPath}' because it does not exist.");
            }

            var fullDstPath = _fs.MapPathInternal(ref newPath, true);

            if (Directory.Exists(fullDstPath))
            {
                throw new FileSystemException($"Cannot move directory because the target path '{newPath}' already exists.");
            }

            _di.MoveTo(newPath);
        }
    }
}