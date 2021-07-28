using System;
using System.IO;

namespace Smartstore.IO
{
    public class NotFoundDirectory : IDirectory
    {
        public NotFoundDirectory(string subpath, IFileSystem fs)
        {
            SubPath = FileSystemBase.NormalizePath(subpath);
            FileSystem = fs;
        }

        public IFileSystem FileSystem { get; }
        public bool Exists => false;
        public bool IsDirectory => true;
        public bool IsRoot => false;
        public IDirectory Parent => null;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public long Length => -1;

        public string SubPath { get; }
        public string Name => SubPath.IsEmpty() ? string.Empty : Path.GetFileName(SubPath);
        public string PhysicalPath => null;

        public bool IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }

        public Stream CreateReadStream() => throw new NotSupportedException();
        public void Delete() => throw new NotSupportedException();
    }
}
