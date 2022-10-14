using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Smartstore.Imaging.Adapters.ImageSharp;
using SharpColor = SixLabors.ImageSharp.Color;
using SharpImageFormat = SixLabors.ImageSharp.Formats.IImageFormat;
using System.Numerics;

namespace Smartstore.Imaging.Barcodes.Impl
{
    internal class ImageGenerator
    {
        private readonly static SharpImageFormat PngFormat = SharpImageFactory.FindInternalImageFormat("png");
        private readonly BarcodeImageOptions _options;

        public ImageGenerator(BarcodeImageOptions options)
        {
            _options = Guard.NotNull(options, nameof(options));

            Guard.IsPositive(options.PixelSize, nameof(options.PixelSize));
            Guard.IsPositive(options.BarHeightFor1DCode, nameof(options.BarHeightFor1DCode));
        }

        public IImage GenerateImage(Barcoder.IBarcode barcode)
        {
            Guard.NotNull(barcode, nameof(barcode));

            // Clone and fix options
            var opts = new BarcodeImageOptions
            {
                BackColor = _options.BackColor,
                BarHeightFor1DCode = _options.BarHeightFor1DCode,
                EanFontFamily = _options.EanFontFamily.NullEmpty() ?? "Arial",
                ForeColor = _options.ForeColor,
                IncludeEanAsText = _options.IncludeEanAsText,
                Margin = _options.Margin ?? barcode.Margin,
                PixelSize = _options.PixelSize
            };

            if (barcode.Bounds.Y == 1)
            {
                return Generate1D(barcode, opts);
            }
                
            else if (barcode.Bounds.Y > 1)
            {
                return Generate2D(barcode, opts);
            }         
            else
            {
                throw new NotSupportedException($"Y value of {barcode.Bounds.Y} is invalid");
            }
        }

        private IImage Generate1D(Barcoder.IBarcode barcode, BarcodeImageOptions o)
        {
            var margin = o.Margin.Value;
            var width = (barcode.Bounds.X + 2 * margin) * o.PixelSize;
            var height = (_options.BarHeightFor1DCode + 2 * margin) * o.PixelSize;

            // Create bitmap.
            var image = new SixLabors.ImageSharp.Image<L8>(width, height);

            image.Mutate(ctx =>
            {
                var background = SharpColor.FromRgba(o.BackColor.R, o.BackColor.G, o.BackColor.B, o.BackColor.A);
                var foreground = SharpColor.FromRgba(o.ForeColor.R, o.ForeColor.G, o.ForeColor.B, o.ForeColor.A);

                ctx.Fill(background);

                for (var x = 0; x < barcode.Bounds.X; x++)
                {
                    if (!barcode.At(x, 0))
                        continue;
                    ctx.FillPolygon(
                        foreground,
                        new Vector2((margin + x) * o.PixelSize, margin * o.PixelSize),
                        new Vector2((margin + x + 1) * o.PixelSize, margin * o.PixelSize),
                        new Vector2((margin + x + 1) * o.PixelSize, (o.BarHeightFor1DCode + margin) * o.PixelSize),
                        new Vector2((margin + x) * o.PixelSize, (o.BarHeightFor1DCode + margin) * o.PixelSize));
                }
            });

            if (_options.IncludeEanAsText && barcode.IsEanBarcode())
            {
                EanTextRenderer.Render(image, barcode, o);
            }

            return new SharpImage(image, PngFormat);
        }

        private IImage Generate2D(Barcoder.IBarcode barcode, BarcodeImageOptions o)
        {
            var margin = o.Margin.Value;
            var width = (barcode.Bounds.X + 2 * margin) * o.PixelSize;
            var height = (barcode.Bounds.Y + 2 * margin) * o.PixelSize;

            // Create bitmap.
            var image = new SixLabors.ImageSharp.Image<L8>(width, height);

            image.Mutate(ctx =>
            {
                var background = SharpColor.FromRgba(o.BackColor.R, o.BackColor.G, o.BackColor.B, o.BackColor.A);
                var foreground = SharpColor.FromRgba(o.ForeColor.R, o.ForeColor.G, o.ForeColor.B, o.ForeColor.A);

                ctx.Fill(background);

                for (var y = 0; y < barcode.Bounds.Y; y++)
                {
                    for (var x = 0; x < barcode.Bounds.X; x++)
                    {
                        if (!barcode.At(x, y))
                            continue;
                        ctx.FillPolygon(
                            foreground,
                            new Vector2((margin + x) * o.PixelSize, (margin + y) * o.PixelSize),
                            new Vector2((margin + x + 1) * o.PixelSize, (margin + y) * o.PixelSize),
                            new Vector2((margin + x + 1) * o.PixelSize, (margin + y + 1) * o.PixelSize),
                            new Vector2((margin + x) * o.PixelSize, (margin + y + 1) * o.PixelSize));
                    }
                }
            });

            return new SharpImage(image, PngFormat);
        }
    }
}
