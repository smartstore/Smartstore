using System.Drawing;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Contains information about a cached image
    /// </summary>
    /// <remarks>
    /// An instance of this object is always returned, even when
    /// the requested image does not physically exists in the storage.
    /// </remarks>
    public class CachedImage
    {
        private bool? _exists;
        private string _mimeType;
        private Size? _size;

        public CachedImage(IFile file)
        {
            File = Guard.NotNull(file, nameof(file));
        }

        /// <summary>
        /// The abstracted file object
        /// </summary>
        public IFile File { get; internal set; }

        /// <summary>
        /// <c>true</c> when the image exists in the cache, <c>false</c> otherwise.
        /// </summary>
        public bool Exists
        {
            get => _exists ?? (_exists = File.Exists).Value;
            // For internal use
            set => _exists = value;
        }

        /// <summary>
        /// The name of the file (without path)
        /// </summary>
        public string FileName
            => System.IO.Path.GetFileName(this.Path);

        public long FileSize
            => !Exists ? 0 : File.Length;

        /// <summary>
        /// The file extension (without 'dot')
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// The filemime type
        /// </summary>
        public string MimeType
            => _mimeType ??= MimeTypes.MapNameToMimeType(FileName);

        /// <summary>
        /// The image width and height.
        /// </summary>
        public Size PixelSize
        {
            get => _size ?? (_size = File.GetPixelSize()).Value;
            internal set => _size = value;
        }

        /// <summary>
        /// The path relative to the cache root folder
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The last modified date or <c>null</c> if the file does not exist
        /// </summary>
        public DateTimeOffset? LastModifiedUtc
            => Exists ? File.LastModified : null;

        /// <summary>
        /// Checks whether the file is remote (outside the application's physical root)
        /// </summary>
        public bool IsRemote { get; set; }
    }
}