using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Smartstore.Core.Content.Media.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImage : Disposable, IProcessableImage
    {
        private readonly Image _image;
        private SharpImageFormat _format;

        public SharpImage(Image image, SixLabors.ImageSharp.Formats.IImageFormat format)
        {
            _image = image;
            _format = new SharpImageFormat(format);

            SourceFormat = _format;
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
        public IImageFormat SourceFormat { get; }

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
        public IImage Save(Stream stream)
        {
            _image.Save(stream, _format.WrappedFormat);
            return this;
        }

        /// <inheritdoc/>
        public async Task<IImage> SaveAsync(Stream stream)
        {
            await _image.SaveAsync(stream, _format.WrappedFormat);
            return this;
        }

        /// <inheritdoc/>
        public IImage Save(string path)
        {
            _image.Save(path);
            return this;
        }

        /// <inheritdoc/>
        public async Task<IImage> SaveAsync(string path)
        {
            await _image.SaveAsync(path);
            return this;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _image.Dispose();
        }
    }
}
