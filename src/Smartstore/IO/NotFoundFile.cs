using System.Drawing;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.IO
{
    public class NotFoundFile : IFile
    {
        private string _dir;

        public NotFoundFile(string subpath, IFileSystem fs)
        {
            SubPath = subpath;
            FileSystem = fs;
        }

        public IFileSystem FileSystem { get; }
        public bool Exists => false;
        public bool IsDirectory => false;
        public DateTimeOffset CreatedOn => DateTimeOffset.MinValue;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public long Length => -1;

        public string SubPath { get; }
        public string Directory => _dir ??= SubPath.IsEmpty() ? string.Empty : SubPath[..(SubPath.Length - Name.Length - 1)];
        public string Name => SubPath.IsEmpty() ? string.Empty : Path.GetFileName(SubPath);
        public string NameWithoutExtension => SubPath.IsEmpty() ? string.Empty : Path.GetFileNameWithoutExtension(SubPath);
        public string Extension => SubPath.IsEmpty() ? string.Empty : Path.GetExtension(SubPath);
        public string PhysicalPath => null;

        public Size GetPixelSize()
            => Size.Empty;

        public bool IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }

        Stream IFileInfo.CreateReadStream()
            => throw new FileNotFoundException($"File '{SubPath}' not found.");

        void IFileEntry.Delete()
            => throw new FileNotFoundException($"File '{SubPath}' not found.");

        void IFileEntry.MoveTo(string newPath)
            => throw new FileNotFoundException($"File '{SubPath}' not found.");

        Stream IFile.OpenRead()
            => throw new FileNotFoundException($"File '{SubPath}' not found.");

        Stream IFile.OpenWrite(string contentType)
            => throw new NotSupportedException();

        IFile IFile.CopyTo(string newPath, bool overwrite)
            => throw new FileNotFoundException($"File '{SubPath}' not found.");

        void IFile.Create(Stream inStream, bool overwrite)
            => throw new NotSupportedException();
    }
}
