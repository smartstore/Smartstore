using System.Drawing;

namespace Smartstore.Imaging
{
    public class ImageWrapper : Disposable, IImage
    {
        private readonly bool _disposeStream;

        public ImageWrapper(Stream stream, Size size, IImageFormat format, byte bitDepth = 24, bool disposeStream = true)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(format, nameof(format));

            InStream = stream;
            Format = format;
            Width = size.Width;
            Height = size.Height;
            SourceSize = size;
            BitDepth = bitDepth;
            _disposeStream = disposeStream;
        }

        #region IImageInfo

        /// <inheritdoc/>
        public int Width { get; }

        /// <inheritdoc/>
        public int Height { get; }

        /// <inheritdoc/>
        public byte BitDepth { get; set; } = 24;

        /// <inheritdoc/>
        public IImageFormat Format { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ImageMetadataEntry> GetMetadata()
            => Enumerable.Empty<ImageMetadataEntry>();

        #endregion

        public Stream InStream { get; }

        /// <inheritdoc/>
        public Size SourceSize { get; set; }

        /// <inheritdoc/>
        public IImage Save(Stream stream, IImageFormat format = null)
            => SaveInternal(stream, format, false).Await();

        /// <inheritdoc/>
        public Task<IImage> SaveAsync(Stream stream, IImageFormat format = null)
            => SaveInternal(stream, format, true);

        private async Task<IImage> SaveInternal(Stream stream, IImageFormat format, bool async)
        {
            Guard.NotNull(stream, nameof(stream));

            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            if (async)
            {
                await InStream.CopyToAsync(stream);
            }
            else
            {
                InStream.CopyTo(stream);
            }

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            if (InStream.CanSeek)
            {
                InStream.Position = 0;
            }

            return this;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing && _disposeStream)
                InStream.Dispose();
        }
    }
}
