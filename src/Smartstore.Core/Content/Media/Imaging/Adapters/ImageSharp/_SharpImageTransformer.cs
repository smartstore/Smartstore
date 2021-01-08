//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using SixLabors.ImageSharp;
//using SixLabors.ImageSharp.Processing;
//using Color = System.Drawing.Color;
//using Size = System.Drawing.Size;
//using Rectangle = System.Drawing.Rectangle;

//namespace Smartstore.Core.Content.Media.Imaging.Adapters.ImageSharp
//{
//    internal sealed class SharpImageTransformer : IImageTransformer
//    {
//        private readonly IImageProcessingContext _op;

//        public SharpImageTransformer(IImageProcessingContext op)
//        {
//            _op = op;
//        }

//        public IImageTransformer Resize(Size size)
//        {
//            return Resize(new ResizeOptions { Size = size });
//        }

//        public IImageTransformer Resize(ResizeOptions options)
//        {
//            Guard.NotNull(options, nameof(options));

//            //var layer = new ResizeOptions(
//            //    size: options.Size,
//            //    resizeMode: (ImageProcessor.Imaging.ResizeMode)options.ResizeMode,
//            //    anchorPosition: (ImageProcessor.Imaging.AnchorPosition)options.AnchorPosition,
//            //    upscale: options.Upscale,
//            //    centerCoordinates: options.CenterCoordinates,
//            //    maxSize: null,
//            //    restrictedSizes: null,
//            //    anchorPoint: options.AnchorPoint);

//            var layer = new SixLabors.ImageSharp.Processing.ResizeOptions
//            {
//                Size = new SixLabors.ImageSharp.Size(options.Size.Width, options.Size.Height),
//                Mode = (SixLabors.ImageSharp.Processing.ResizeMode)options.ResizeMode,
//                Position = (SixLabors.ImageSharp.Processing.AnchorPositionMode)options.AnchorPosition
//            };

//            _op.Resize(layer);
//            return this;
//        }

//        public IImageTransformer BackgroundColor(Color color)
//        {
//            _op.BackgroundColor(color);
//            return this;
//        }

//        public IImageTransformer Opacity(float amount, Rectangle rect)
//        {
//            _op.Opacity(amount, rect);
//            return this;
//        }

//        public IImageTransformer Brightness(int percentage)
//        {
//            _op.Brightness(percentage);
//            return this;
//        }

//        public IImageTransformer Contrast(int percentage)
//        {
//            _op.Contrast(percentage);
//            return this;
//        }

//        public IImageTransformer Crop(Rectangle rect)
//        {
//            _op.Crop(rect);
//            return this;
//        }

//        public IImageTransformer EntropyCrop(byte threshold = 128)
//        {
//            _op.EntropyCrop(threshold);
//            return this;
//        }

//        public IImageTransformer Flip(bool flipVertically = false, bool flipBoth = false)
//        {
//            _op.Flip(flipVertically, flipBoth);
//            return this;
//        }

//        public IImageTransformer Format(IImageFormat format)
//        {
//            if (format is SharpImageFormat sharpFormat)
//            {
//                _image.Format(sharpFormat.WrappedFormat);
//                return this;
//            }

//            throw new ArgumentException("Passed image type must be '{0}'.".FormatCurrent(typeof(SharpImageFormat)), nameof(format));
//        }

//        public IImageTransformer Gamma(float value)
//        {
//            _op.Gamma(value);
//            return this;
//        }

//        public IImageTransformer GaussianBlur(int size)
//        {
//            _op.GaussianBlur(size);
//            return this;
//        }

//        public IImageTransformer GaussianSharpen(int size)
//        {
//            _op.GaussianSharpen(size);
//            return this;
//        }

//        public IImageTransformer Hue(int degrees, bool rotate = false)
//        {
//            _op.Hue(degrees, rotate);
//            return this;
//        }

//        public IImageTransformer Quality(int percentage)
//        {
//            _op.Quality(percentage);
//            return this;
//        }

//        public IImageTransformer BitDepth(BitDepth bitDepth)
//        {
//            _op.BitDepth((long)bitDepth);
//            return this;
//        }

//        public IImageTransformer Overlay(IImage image, Size size, int opacity, Point? position)
//        {
//            if (image is IPImage ipImage)
//            {
//                _image.Overlay(new ImageLayer
//                {
//                    Image = ipImage.ImageFactory.Image,
//                    Size = size,
//                    Opacity = opacity,
//                    Position = position
//                });

//                return this;
//            }

//            throw new ArgumentException("Passed image type must be '{0}'.".FormatCurrent(typeof(IPImage)), nameof(image));
//        }

//        public IImageTransformer Mask(IImage image, Size size, int opacity, Point? position)
//        {
//            if (image is IPImage ipImage)
//            {
//                _image.Mask(new ImageLayer
//                {
//                    Image = ipImage.ImageFactory.Image,
//                    Size = size,
//                    Opacity = opacity,
//                    Position = position
//                });

//                return this;
//            }

//            throw new ArgumentException("Passed image type must be '{0}'.".FormatCurrent(typeof(IPImage)), nameof(image));
//        }

//        public IImageTransformer Rotate(float degrees, bool bounded = false, bool keepSize = false)
//        {
//            if (bounded)
//                _op.Rotate(degrees);
//            else
//                _op.RotateBounded(degrees, keepSize);

//            return this;
//        }

//        public IImageTransformer RoundedCorners(int radius)
//        {
//            _op.RoundedCorners(radius);
//            return this;
//        }

//        public IImageTransformer Saturation(int percentage)
//        {
//            _op.Saturation(percentage);
//            return this;
//        }

//        public IImageTransformer Tint(Color color)
//        {
//            _op.Tint(color);
//            return this;
//        }

//        public IImageTransformer Vignette(Color? color = null)
//        {
//            _op.Vignette(color);
//            return this;
//        }
//    }
//}
