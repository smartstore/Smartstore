using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.FileProviders;
using Smartstore.Engine;
using Smartstore.Imaging;
using Smartstore.IO.SymLinks;

namespace Smartstore.IO
{
    public class LocalFile : IFile
    {
        private readonly FileInfo _fi;

        private string _ext;
        private string _title;
        private string _dir;
        private Size? _size;

        public LocalFile(string subpath, FileInfo info, IFileSystem fileSystem)
        {
            _fi = info;

            FileSystem = fileSystem;
            SubPath = FileSystemBase.NormalizePath(subpath);
        }

        public FileInfo AsFileInfo() => _fi;

        /// <inheritdoc/>
        public IFileSystem FileSystem { get; protected internal set; }

        /// <inheritdoc/>
        public string SubPath { get; }

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
            get => _ext ??= (_fi?.Extension ?? System.IO.Path.GetExtension(PhysicalPath));
        }

        /// <inheritdoc/>
        public Size Size
        {
            get
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
            internal set => _size = value;
        }

        public bool IsSymbolicLink(out string finalPhysicalPath)
            => _fi.IsSymbolicLink(out finalPhysicalPath);

        Stream IFileInfo.CreateReadStream()
            => OpenRead();

        /// <inheritdoc/>
        public Task<Stream> OpenReadAsync()
            => Task.FromResult(OpenRead());

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
        public Stream OpenWrite()
        {
            var di = _fi?.Directory;
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
                FileMode.OpenOrCreate, 
                FileAccess.Write, 
                FileShare.None, 
                bufferSize: 4096, 
                useAsync: true);
        }
    }
}