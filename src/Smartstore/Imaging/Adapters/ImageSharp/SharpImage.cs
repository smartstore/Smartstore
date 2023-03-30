using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Size = System.Drawing.Size;

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImage : Disposable, IProcessableImage
    {
        internal readonly static SharpImageFormat PngFormat = 
            ImageSharpUtility.CreateFormat(SharpImageFactory.FindInternalImageFormat("png"));

        private readonly Image _image;
        private SharpImageFormat _format;

        public SharpImage(Image image)
        {
            _image = Guard.NotNull(image);
            _format = image.Metadata.DecodedImageFormat != null
                ? ImageSharpUtility.CreateFormat(image.Metadata.DecodedImageFormat)
                : PngFormat;
            SourceSize = new Size(image.Width, image.Height);
        }

        public SharpImage(Image image, SharpImageFormat format)
        {
            _image = Guard.NotNull(image);
            _format = Guard.NotNull(format);
            SourceSize = new Size(image.Width, image.Height);
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
                Guard.NotNull(value);
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
