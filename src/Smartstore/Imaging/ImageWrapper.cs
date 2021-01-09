using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

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

        #endregion

        public Stream InStream { get; }

        /// <inheritdoc/>
        public Size SourceSize { get; set; }

        /// <inheritdoc/>
        public IImage Save(Stream stream, IImageFormat format = null)
        {
            Guard.NotNull(stream, nameof(stream));

            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            InStream.CopyTo(stream);

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

        /// <inheritdoc/>
        public async Task<IImage> SaveAsync(Stream stream, IImageFormat format = null)
        {
            Guard.NotNull(stream, nameof(stream));

            if (stream.CanSeek)
            {
                stream.SetLength(0);
            }

            await InStream.CopyToAsync(stream);

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
