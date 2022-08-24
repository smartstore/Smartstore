using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.FileProviders;
using Smartstore.Imaging;

namespace Smartstore.IO
{
    [DebuggerDisplay("LocalFile: {SubPath}")]
    public class LocalFile : IFile
    {
        private readonly FileInfo _fi;

        private LocalFileSystem _fs;
        private string _ext;
        private string _title;
        private string _dir;
        private Size? _size;

        public LocalFile(string subpath, FileInfo info, LocalFileSystem fileSystem)
        {
            _fi = info;
            _fs = fileSystem;

            FileSystem = fileSystem;
            SubPath = subpath;
        }

        public FileInfo AsFileInfo() => _fi;

        /// <inheritdoc/>
        public IFileSystem FileSystem
        {
            get => _fs;
            protected internal set => _fs = Guard.NotNull(value as LocalFileSystem, nameof(value));
        }

        /// <inheritdoc/>
        public string SubPath { get; internal set; }

        /// <inheritdoc />
        public bool Exists
        {
            get => _fi.Exists;
        }

        /// <inheritdoc />
        public long Length
        {
            get => _fi.Length;
        }

        /// <inheritdoc />
        public string PhysicalPath
        {
            get => _fi.FullName;
        }

        /// <inheritdoc />
        public string Name
        {
            get => _fi.Name;
        }

        /// <inheritdoc />
        public DateTimeOffset CreatedOn
        {
            get => _fi.CreationTimeUtc;
        }

        /// <inheritdoc />
        public DateTimeOffset LastModified
        {
            get => _fi.LastWriteTimeUtc;
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsDirectory
        {
            get => false;
        }

        /// <inheritdoc/>
        public string Directory
        {
            get => _dir ??= SubPath.Substring(0, SubPath.Length - Name.Length);
        }

        /// <inheritdoc/>
        public string NameWithoutExtension
        {
            get => _title ??= (PhysicalPath == null ? string.Empty : System.IO.Path.GetFileNameWithoutExtension(PhysicalPath));
        }

        /// <inheritdoc/>
        public string Extension
        {
            get => _ext ??= (_fi.Extension ?? System.IO.Path.GetExtension(PhysicalPath));
        }

        /// <inheritdoc/>
        public Size GetPixelSize()
        {
            if (_size == null)
            {
                if (!Exists)
                {
                    _size = Size.Empty;
                    return _size.Value;
                }

                try
                {
                    var mime = MimeTypes.MapNameToMimeType(Name);
                    if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    {
                        _size = ImageHeader.GetPixelSize(OpenRead(), mime, false);
                    }

                    // Don't attemp again
                    _size ??= Size.Empty;
                }
                catch
                {
                    _size = Size.Empty;
                }
            }

            return _size.Value;
        }

        public bool IsSymbolicLink(out string finalTargetPath)
        {
            var linkTarget = _fi.ResolveLinkTarget(true);
            finalTargetPath = linkTarget?.FullName;
            return linkTarget != null;
        }

        Stream IFileInfo.CreateReadStream()
            => OpenRead();

        /// <inheritdoc/>
        public Stream OpenRead()
        {
            if (!Exists)
            {
                throw new FileNotFoundException(PhysicalPath ?? SubPath);
            }

            // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
            // 0 causes constructor to throw
            int bufferSize = 1;
            return new FileStream(
                PhysicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        /// <inheritdoc/>
        public Stream OpenWrite(string contentType = null)
        {
            var di = _fi.Directory;
            if (di == null && PhysicalPath.HasValue())
            {
                di = new DirectoryInfo(Path.GetDirectoryName(PhysicalPath));
            }

            if (di == null)
            {
                throw new FileSystemException($"Cannot create directory for file path '{SubPath}'.");
            }

            if (!di.Exists)
            {
                System.IO.Directory.CreateDirectory(di.FullName);
            }

            return new FileStream(
                PhysicalPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
        }


        public void Delete()
        {
            _fi.WaitForUnlockAndExecute(x => x.Delete());
        }

        public IFile CopyTo(string newPath, bool overwrite)
        {
            Guard.NotNull(newPath, nameof(newPath));

            if (!_fi.Exists)
            {
                throw new FileNotFoundException($"The file '{SubPath}' does not exist.");
            }

            var fullDstPath = _fs.MapPathInternal(ref newPath, true, true);

            if (System.IO.Directory.Exists(fullDstPath))
            {
                throw new FileSystemException($"Cannot copy file to '{newPath}' because it already exists as a directory.");
            }

            if (!overwrite && File.Exists(fullDstPath))
            {
                throw new FileSystemException($"Cannot copy file because the destination path '{newPath}' already exists.");
            }

            FileInfo copy = null;
            _fi.WaitForUnlockAndExecute(x =>
            {
                copy = x.CopyTo(fullDstPath, overwrite);
            });

            return new LocalFile(newPath, copy, _fs);
        }

        public void MoveTo(string newPath)
        {
            Guard.NotNull(newPath, nameof(newPath));

            if (!_fi.Exists)
            {
                throw new FileNotFoundException($"Cannot move file '{SubPath}' because it does not exist.");
            }

            var fullDstPath = _fs.MapPathInternal(ref newPath, true, true);
            _fi.WaitForUnlockAndExecute(x => _fi.MoveTo(fullDstPath, false));

            SubPath = newPath;
        }

        public void Create(Stream inStream, bool overwrite)
            => CreateInternal(inStream, overwrite, false).Await();

        public Task CreateAsync(Stream inStream, bool overwrite, CancellationToken cancelToken = default)
            => CreateInternal(inStream, overwrite, true, cancelToken);

        private async Task CreateInternal(Stream inStream, bool overwrite, bool async, CancellationToken cancelToken = default)
        {
            if (!overwrite && _fi.Exists)
            {
                throw new FileSystemException($"Cannot create file '{_fi.FullName}' because it already exists.");
            }

            // Ensure the directory exists
            var dirName = Path.GetDirectoryName(_fi.FullName);
            if (!System.IO.Directory.Exists(dirName))
            {
                System.IO.Directory.CreateDirectory(dirName);
            }

            inStream ??= new MemoryStream();
            using var outputStream = _fi.Create();

            if (async)
            {
                await inStream.CopyToAsync(outputStream, cancelToken);
            }
            else
            {
                inStream.CopyTo(outputStream);
            }

            _fi.Refresh();
        }
    }
}