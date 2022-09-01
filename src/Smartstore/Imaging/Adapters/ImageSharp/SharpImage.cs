using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SharpFormat = SixLabors.ImageSharp.Formats.IImageFormat;
using Size = System.Drawing.Size;

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImage : Disposable, IProcessableImage
    {
        private readonly Image _image;
        private SharpImageFormat _format;

        public SharpImage(Image image, SharpFormat sharpFormat)
            : this(image, ImageSharpUtility.CreateFormat(sharpFormat))
        {
        }

        private SharpImage(Image image, SharpImageFormat format)
        {
            _image = image;
            _format = format;
            SourceSize = new System.Drawing.Size(image.Width, image.Height);
        }

        #region IImageInfo

        /// <inheritdoc/>
        public int Width
            => _image.Width;

        /// <inheritdoc/>
        public int Height
            => _image.Height;

        /// <inheritdoc/>
        public byte BitDepth
            => (byte)(_image.PixelType?.BitsPerPixel);

        /// <inheritdoc/>
        public IImageFormat Format
        {
            get => _format;
            set
            {
                Guard.NotNull(value, nameof(value));
                Guard.IsTypeOf<SharpImageFormat>(value);

                _format = (SharpImageFormat)value;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<ImageMetadataEntry> GetMetadata()
            => SharpImageInfo.ConvertMetadata(_image.Metadata);

        #endregion

        public Image WrappedImage => _image;

        /// <inheritdoc/>
        public Size SourceSize { get; }

        /// <inheritdoc/>
        public void Transform(Action<IImageTransformer> operation)
        {
            _image.Mutate(x => operation(new SharpImageTransformer(x, this)));
        }

        /// <inheritdoc/>
        public IProcessableImage Clone(Action<IImageTransformer> operation)
        {
            var clone = _image.Clone(x => operation(new SharpImageTransformer(x, this)));
            return new SharpImage(clone, _format);
        }

        /// <inheritdoc/>
        public IImage Save(Stream stream, IImageFormat format = null)
            => SaveInternal(stream, format, false).Await();

        /// <inheritdoc/>
        public Task<IImage> SaveAsync(Stream stream, IImageFormat format = null)
            => SaveInternal(stream, format, true);

        private async Task<IImage> SaveInternal(Stream stream, IImageFormat format, bool async)
        {
            var encoder = ((SharpImageFormat)(format ?? _format)).CreateEncoder();

            if (async)
            {
                await _image.SaveAsync(stream, encoder);
            }
            else
            {
                _image.Save(stream, encoder);
            }

            // Remember format for next save
            if (format != null)
            {
                Format = format;
            }

            return this;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _image.Dispose();
        }
    }
}
