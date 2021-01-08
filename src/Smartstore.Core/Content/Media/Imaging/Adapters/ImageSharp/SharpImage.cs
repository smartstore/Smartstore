using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SharpFormat = SixLabors.ImageSharp.Formats.IImageFormat;

namespace Smartstore.Core.Content.Media.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImage : Disposable, IProcessableImage
    {
        private readonly Image _image;
        private SharpImageFormat _format;

        public SharpImage(Image image, SharpFormat sharpFormat)
        {
            _image = image;
            _format = ImageSharpUtility.CreateFormat(sharpFormat);
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

        #endregion

        public Image WrappedImage => _image;

        /// <inheritdoc/>
        public System.Drawing.Size SourceSize { get; }

        /// <inheritdoc/>
        public IImage Transform(Action<IImageTransformer> transformer)
        {
            //_image.Mutate(x => transformer(new SharpImageTransformer(x)));
            //return this;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IImage Save(Stream stream, IImageFormat format = null)
        {
            var encoder = ((SharpImageFormat)(format ?? _format)).CreateEncoder();
            _image.Save(stream, encoder);

            // Remember format for next save
            if (format != null)
            {
                Format = format;
            }

            return this;
        }

        /// <inheritdoc/>
        public async Task<IImage> SaveAsync(Stream stream, IImageFormat format = null)
        {
            var encoder = ((SharpImageFormat)(format ?? _format)).CreateEncoder();
            await _image.SaveAsync(stream, encoder);

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
