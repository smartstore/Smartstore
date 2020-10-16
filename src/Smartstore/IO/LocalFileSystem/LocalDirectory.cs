using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Smartstore.IO.SymLinks;

namespace Smartstore.IO
{
    public class LocalDirectory : IDirectory
    {
        private readonly DirectoryInfo _di;

        public LocalDirectory(string subpath, DirectoryInfo info)
        {
            _di = info;
            SubPath = FileSystemBase.NormalizePath(subpath);
        }

        public DirectoryInfo AsDirectoryInfo() => _di;

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
                    return new LocalDirectory(Path.GetDirectoryName(SubPath), _di.Parent);
                }

                return null;
            }
        }

        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();
    }
}