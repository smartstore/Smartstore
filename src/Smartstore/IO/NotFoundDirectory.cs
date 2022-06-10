using Microsoft.Extensions.FileProviders;

namespace Smartstore.IO
{
    public class NotFoundDirectory : IDirectory
    {
        public NotFoundDirectory(string subpath, IFileSystem fs)
        {
            SubPath = PathUtility.NormalizeRelativePath(subpath);
            FileSystem = fs;
        }

        public IFileSystem FileSystem { get; }
        public bool Exists => false;
        public bool IsDirectory => true;
        public bool IsRoot => false;
        public IDirectory Parent => null;
        public DateTimeOffset CreatedOn => DateTimeOffset.MinValue;
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

        Stream IFileInfo.CreateReadStream()
            => throw new NotSupportedException();

        void IFileEntry.Delete()
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        void IFileEntry.MoveTo(string newPath)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        void IDirectory.Create()
            => throw new NotSupportedException();

        IDirectory IDirectory.CreateSubdirectory(string path)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        IEnumerable<IFileEntry> IDirectory.EnumerateEntries(string pattern, bool deep)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        IEnumerable<IDirectory> IDirectory.EnumerateDirectories(string pattern, bool deep)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        IEnumerable<IFile> IDirectory.EnumerateFiles(string pattern, bool deep)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        long IDirectory.GetDirectorySize(string pattern, bool deep)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");

        int IDirectory.CountFiles(string pattern, bool deep)
            => throw new DirectoryNotFoundException($"Directory '{SubPath}' not found.");
    }
}
