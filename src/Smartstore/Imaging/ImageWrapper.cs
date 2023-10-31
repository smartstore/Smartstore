using System.Drawing;

namespace Smartstore.Imaging
{
    public class ImageWrapper : Disposable, IImage
    {
        private readonly IImageInfo _info;
        private readonly bool _disposeStream;

        public ImageWrapper(Stream stream, IImageInfo info, bool disposeStream = true)
        {
            Guard.NotNull(stream);
            Guard.NotNull(info);

            InStream = stream;
            Format = info.Format;
            SourceSize = new Size(info.Width, info.Height);

            _info = info;
            _disposeStream = disposeStream;
        }

        #region IImageInfo

        /// <inheritdoc/>
        public int Width => _info.Width;

        /// <inheritdoc/>
        public int Height => _info.Height;

        /// <inheritdoc/>
        public byte BitDepth => _info.BitDepth;

        /// <inheritdoc/>
        public IImageFormat Format { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ImageMetadataEntry> GetMetadata()
            => _info.GetMetadata();

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
            Guard.NotNull(stream);

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
