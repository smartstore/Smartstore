using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
#region Gdi vs. ImageSharp
using Size = System.Drawing.Size;
using SharpSize = SixLabors.ImageSharp.Size;
using Color = System.Drawing.Color;
using SharpColor = SixLabors.ImageSharp.Color;
using Rectangle = System.Drawing.Rectangle;
using SharpRectangle = SixLabors.ImageSharp.Rectangle;
using Point = System.Drawing.Point;
using SharpPoint = SixLabors.ImageSharp.Point;
using SharpPointF = SixLabors.ImageSharp.PointF;
using SharpResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using SharpAnchorPositionMode = SixLabors.ImageSharp.Processing.AnchorPositionMode;
using SharpResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;
using SharpFlipMode = SixLabors.ImageSharp.Processing.FlipMode;
using SharpPixelAlphaCompositionMode = SixLabors.ImageSharp.PixelFormats.PixelAlphaCompositionMode;
using SharpPixelColorBlendingMode = SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode;
using System.Collections.Frozen;
#endregion

namespace Smartstore.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImageTransformer : IImageTransformer
    {
        private readonly static FrozenDictionary<DitheringMode, Func<IDither>> _dithererMap = new Dictionary<DitheringMode, Func<IDither>>()
        {
            [DitheringMode.Atkinson] = () => KnownDitherings.Atkinson,
            [DitheringMode.Bayer8x8] = () => KnownDitherings.Bayer8x8,
            [DitheringMode.Burks] = () => KnownDitherings.Burks,
            [DitheringMode.FloydSteinberg] = () => KnownDitherings.FloydSteinberg,
            [DitheringMode.Ordered3x3] = () => KnownDitherings.Ordered3x3,
            [DitheringMode.Stucki] = () => KnownDitherings.Stucki
        }.ToFrozenDictionary();

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



        public IImageTransformer AutoOrient()
        {
            _context.AutoOrient();
            return this;
        }

        public IImageTransformer BlackWhite()
        {
            _context.BlackWhite();
            return this;
        }

        public IImageTransformer BokehBlur(int radius = 32, int components = 2, float gamma = 3F)
        {
            _context.BokehBlur(radius, components, gamma);
            return this;
        }

        public IImageTransformer BoxBlur(int radius = 7)
        {
            _context.BoxBlur(radius);
            return this;
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

        public IImageTransformer Opacity(float amount)
        {
            _context.Opacity(amount);
            return this;
        }

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

        public IImageTransformer Dither(DitheringMode mode = DitheringMode.FloydSteinberg, float ditherScale = 1F)
        {
            _context.Dither(_dithererMap[mode](), ditherScale);
            return this;
        }

        public IImageTransformer EntropyCrop(float threshold = .5f)
        {
            _context.EntropyCrop(threshold);
            return this;
        }

        public IImageTransformer Flip(FlipMode mode)
        {
            _context.Flip((SharpFlipMode)mode);
            return this;
        }

        public IImageTransformer GaussianBlur(float sigma = 3f)
        {
            _context.GaussianBlur(sigma);
            return this;
        }

        public IImageTransformer GaussianSharpen(float sigma = 3f)
        {
            _context.GaussianSharpen(sigma);
            return this;
        }

        public IImageTransformer Grayscale(float amount = 1F)
        {
            _context.Grayscale(amount);
            return this;
        }

        public IImageTransformer Hue(float degrees)
        {
            _context.Hue(degrees);
            return this;
        }

        public IImageTransformer Invert()
        {
            _context.Invert();
            return this;
        }

        public IImageTransformer Kodachrome()
        {
            _context.Kodachrome();
            return this;
        }

        public IImageTransformer Lightness(float amount)
        {
            _context.Lightness(amount);
            return this;
        }

        public IImageTransformer OilPaint(int levels = 10, int brushSize = 15)
        {
            _context.OilPaint(levels, brushSize);
            return this;
        }

        public IImageTransformer Sepia(float amount = 1F)
        {
            _context.Sepia(amount);
            return this;
        }

        public IImageTransformer DrawImage(IImage image,
            Point position,
            PixelColorBlendingMode colorBlending,
            PixelAlphaCompositionMode alphaComposition,
            float opacity)
        {
            Guard.NotNull(image, nameof(image));

            var sharpImage = (image as SharpImage)?.WrappedImage;
            if (sharpImage is null)
            {
                using (var stream = new MemoryStream())
                {
                    image.Save(stream);
                    sharpImage = SixLabors.ImageSharp.Image.Load(stream);
                }
            }

            var locationSize = (Size)position;
            var locationPoint = new SharpPoint(new SharpSize(locationSize.Width, locationSize.Height));

            _context.DrawImage(sharpImage,
                locationPoint,
                (SharpPixelColorBlendingMode)colorBlending,
                (SharpPixelAlphaCompositionMode)alphaComposition,
                opacity);

            return this;
        }

        public IImageTransformer Rotate(float degrees)
        {
            _context.Rotate(degrees);
            return this;
        }

        public IImageTransformer Saturate(float amount)
        {
            _context.Saturate(amount);
            return this;
        }

        public IImageTransformer Vignette(Color? color = null)
        {
            if (color == null)
            {
                _context.Vignette();
            }
            else
            {
                _context.Vignette(SharpColor.FromRgba(color.Value.R, color.Value.G, color.Value.B, color.Value.A));
            }

            return this;
        }

        public IImageTransformer Vignette(Color color, float radiusX, float radiusY, Rectangle rect)
        {
            _context.Vignette(
                SharpColor.FromRgba(color.R, color.G, color.B, color.A),
                radiusX,
                radiusY,
                new SharpRectangle(rect.X, rect.Y, rect.Width, rect.Height));

            return this;
        }
    }
}