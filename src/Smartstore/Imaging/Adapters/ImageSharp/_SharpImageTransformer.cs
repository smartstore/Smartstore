using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
#region Gdi vs. ImageSharp
using Size = System.Drawing.Size;
using SharpSize = SixLabors.ImageSharp.Size;
using Color = System.Drawing.Color;
using SharpColor = SixLabors.ImageSharp.Color;
using Rectangle = System.Drawing.Rectangle;
using SharpRectangle = SixLabors.ImageSharp.Rectangle;
using PointF = System.Drawing.PointF;
using SharpPointF = SixLabors.ImageSharp.PointF;
using SharpResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using SharpAnchorPositionMode = SixLabors.ImageSharp.Processing.AnchorPositionMode;
using SharpResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;
using SixLabors.ImageSharp.Memory;
#endregion

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImageTransformer : IImageTransformer
    {
        private readonly IImageProcessingContext _context;

        public SharpImageTransformer(IImageProcessingContext context, IProcessableImage image)
        {
            _context = context;
            Image = image;
        }

        /// <summary>
        /// Gets the current processed image.
        /// </summary>
        public IProcessableImage Image { get; }

        /// <summary>
        /// Gets the image dimensions at the current point in the processing pipeline.
        /// </summary>
        public Size CurrentSize 
        {
            get
            {
                var sharpSize = _context.GetCurrentSize();
                return new Size(sharpSize.Width, sharpSize.Height);
            }
        }

        public IImageTransformer Resize(ResizeOptions options)
        {
            Guard.NotNull(options, nameof(options));

            var size = options.Size;
            if (options.Mode == ResizeMode.Max && size.Width > 0 && size.Width == size.Height)
            {
                size = ImagingHelper.Rescale(CurrentSize, size);
            }

            var sharpResizeOptions = new SharpResizeOptions
            {
                Size = new SharpSize(size.Width, size.Height),
                Mode = (SharpResizeMode)options.Mode,
                Position = (SharpAnchorPositionMode)options.Position,
                Compand = options.Compand
            };

            if (options.Resampling != ResamplingMode.Bicubic)
            {
                sharpResizeOptions.Sampler = ImageSharpUtility.GetResampler(options.Resampling);
            }

            if (options.CenterCoordinates != null)
            {
                var c = options.CenterCoordinates.Value;
                sharpResizeOptions.CenterCoordinates = new SharpPointF(c.X, c.Y);
            }

            if (options.TargetRectangle != null)
            {
                var r = options.TargetRectangle.Value;
                sharpResizeOptions.TargetRectangle = new SharpRectangle(r.X, r.Y, r.Width, r.Height);
            }

            _context.Resize(sharpResizeOptions);
            return this;
        }

        public IImageTransformer BackgroundColor(Color color)
        {
            _context.BackgroundColor(SharpColor.FromRgba(color.R, color.G, color.B, color.A));
            return this;
        }

        //public IImageTransformer Opacity(float amount, Rectangle rect)
        //{
        //    _op.Opacity(amount, rect);
        //    return this;
        //}

        public IImageTransformer Brightness(float amount)
        {
            _context.Brightness(amount);
            return this;
        }

        public IImageTransformer Contrast(float amount)
        {
            _context.Contrast(amount);
            return this;
        }

        public IImageTransformer Crop(Rectangle rect)
        {
            _context.Crop(new SharpRectangle(rect.X, rect.Y, rect.Width, rect.Height));
            return this;
        }

        //public IImageTransformer EntropyCrop(byte threshold = 128)
        //{
        //    _op.EntropyCrop(threshold);
        //    return this;
        //}

        //public IImageTransformer Flip(bool flipVertically = false, bool flipBoth = false)
        //{
        //    _op.Flip(flipVertically, flipBoth);
        //    return this;
        //}

        //public IImageTransformer GaussianBlur(int size)
        //{
        //    _op.GaussianBlur(size);
        //    return this;
        //}

        //public IImageTransformer GaussianSharpen(int size)
        //{
        //    _op.GaussianSharpen(size);
        //    return this;
        //}

        //public IImageTransformer Hue(int degrees, bool rotate = false)
        //{
        //    _op.Hue(degrees, rotate);
        //    return this;
        //}

        //public IImageTransformer BitDepth(BitDepth bitDepth)
        //{
        //    _op.BitDepth((long)bitDepth);
        //    return this;
        //}

        //public IImageTransformer Overlay(IImage image, Size size, int opacity, Point? position)
        //{
        //    if (image is IPImage ipImage)
        //    {
        //        _image.Overlay(new ImageLayer
        //        {
        //            Image = ipImage.ImageFactory.Image,
        //            Size = size,
        //            Opacity = opacity,
        //            Position = position
        //        });

        //        return this;
        //    }

        //    throw new ArgumentException("Passed image type must be '{0}'.".FormatCurrent(typeof(IPImage)), nameof(image));
        //}

        //public IImageTransformer Mask(IImage image, Size size, int opacity, Point? position)
        //{
        //    if (image is IPImage ipImage)
        //    {
        //        _image.Mask(new ImageLayer
        //        {
        //            Image = ipImage.ImageFactory.Image,
        //            Size = size,
        //            Opacity = opacity,
        //            Position = position
        //        });

        //        return this;
        //    }

        //    throw new ArgumentException("Passed image type must be '{0}'.".FormatCurrent(typeof(IPImage)), nameof(image));
        //}

        //public IImageTransformer Rotate(float degrees, bool bounded = false, bool keepSize = false)
        //{
        //    if (bounded)
        //        _op.Rotate(degrees);
        //    else
        //        _op.RotateBounded(degrees, keepSize);

        //    return this;
        //}

        //public IImageTransformer RoundedCorners(int radius)
        //{
        //    _op.RoundedCorners(radius);
        //    return this;
        //}

        //public IImageTransformer Saturation(int percentage)
        //{
        //    _op.Saturation(percentage);
        //    return this;
        //}

        //public IImageTransformer Tint(Color color)
        //{
        //    _op.Tint(color);
        //    return this;
        //}

        //public IImageTransformer Vignette(Color? color = null)
        //{
        //    _op.Vignette(color);
        //    return this;
        //}
    }
}
