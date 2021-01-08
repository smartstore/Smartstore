using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.Core.Content.Media.Imaging
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
        public IImage Save(string path)
        {
            Guard.NotEmpty(path, nameof(path));

            var di = new DirectoryInfo(Path.GetDirectoryName(path));
            if (!di.Exists)
            {
                di.Create();
            }

            using var stream = File.OpenWrite(path);
            return Save(stream);
        }

        /// <inheritdoc/>
        public Task<IImage> SaveAsync(string path)
        {
            Guard.NotEmpty(path, nameof(path));

            var di = new DirectoryInfo(Path.GetDirectoryName(path));
            if (!di.Exists)
            {
                di.Create();
            }

            using var stream = File.OpenWrite(path);
            return SaveAsync(stream);
        }

        /// <inheritdoc/>
        public IImage Save(Stream stream)
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
        public async Task<IImage> SaveAsync(Stream stream)
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
