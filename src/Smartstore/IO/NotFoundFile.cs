using System;
using System.Drawing;
using System.IO;

namespace Smartstore.IO
{
    public class NotFoundFile : IFile
    {
        public NotFoundFile(string subpath)
        {
            SubPath = FileSystemBase.NormalizePath(subpath);
        }

        public bool Exists => false;
        public bool IsDirectory => false;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public long Length => -1;
        public Size Size => Size.Empty;

        public string SubPath { get; }
        public string Directory => null;
        public string Name => SubPath.IsEmpty() ? string.Empty : Path.GetFileName(SubPath);
        public string NameWithoutExtension => SubPath.IsEmpty() ? string.Empty : Path.GetFileNameWithoutExtension(SubPath);
        public string Extension => SubPath.IsEmpty() ? string.Empty : Path.GetExtension(SubPath);
        public string PhysicalPath => null;

        public bool IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }

        public Stream CreateReadStream() => throw new NotSupportedException();
        public Stream OpenRead() => throw new NotSupportedException();
        public Stream OpenWrite() => throw new NotSupportedException();
    }
}
