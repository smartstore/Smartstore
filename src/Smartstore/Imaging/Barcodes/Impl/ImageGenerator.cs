using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Smartstore.Imaging.Adapters.ImageSharp;
using SharpColor = SixLabors.ImageSharp.Color;

namespace Smartstore.Imaging.Barcodes.Impl
{
    internal class ImageGenerator
    {
        private readonly BarcodeImageOptions _options;

        public ImageGenerator(BarcodeImageOptions options)
        {
            _options = Guard.NotNull(options);

            if (options.Margin.HasValue)
            {
                Guard.NotNegative(options.Margin.Value);
            }

            Guard.IsPositive(options.Scale);
            Guard.IsPositive(options.BarHeightFor1DCode);
        }

        public IImage GenerateImage(Barcoder.IBarcode barcode)
        {
            Guard.NotNull(barcode);

            // Clone and fix options
            var opts = new ImageOptions(_options, barcode);

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

        private static IImage Generate1D(Barcoder.IBarcode barcode, ImageOptions o)
        {
            var width = (barcode.Bounds.X + 2 * o.Margin) * o.Scale;
            var height = (o.BarHeightFor1DCode + 2 * o.Margin) * o.Scale;

            // Create bitmap.
            var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                ctx.Fill(o.BackColor);

                for (var x = 0; x < barcode.Bounds.X; x++)
                {
                    if (!barcode.At(x, 0))
                        continue;
                    ctx.FillPolygon(
                        o.ForeColor,
                        new Vector2((o.Margin + x) * o.Scale, o.Margin * o.Scale),
                        new Vector2((o.Margin + x + 1) * o.Scale, o.Margin * o.Scale),
                        new Vector2((o.Margin + x + 1) * o.Scale, (o.BarHeightFor1DCode + o.Margin) * o.Scale),
                        new Vector2((o.Margin + x) * o.Scale, (o.BarHeightFor1DCode + o.Margin) * o.Scale));
                }
            });

            if (o.IncludeEanAsText && barcode.IsEanBarcode())
            {
                EanTextRenderer.Render(image, barcode, o);
            }
            
            return new SharpImage(image);
        }

        private static IImage Generate2D(Barcoder.IBarcode barcode, ImageOptions o)
        {
            var width = (barcode.Bounds.X + 2 * o.Margin) * o.Scale;
            var height = (barcode.Bounds.Y + 2 * o.Margin) * o.Scale;

            // Create bitmap.
            var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                ctx.Fill(o.BackColor);

                for (var y = 0; y < barcode.Bounds.Y; y++)
                {
                    for (var x = 0; x < barcode.Bounds.X; x++)
                    {
                        if (!barcode.At(x, y))
                            continue;
                        ctx.FillPolygon(
                            o.ForeColor,
                            new Vector2((o.Margin + x) * o.Scale, (o.Margin + y) * o.Scale),
                            new Vector2((o.Margin + x + 1) * o.Scale, (o.Margin + y) * o.Scale),
                            new Vector2((o.Margin + x + 1) * o.Scale, (o.Margin + y + 1) * o.Scale),
                            new Vector2((o.Margin + x) * o.Scale, (o.Margin + y + 1) * o.Scale));
                    }
                }
            });

            return new SharpImage(image);
        }

        #region ImageOptions

        readonly struct ImageOptions
        {
            public ImageOptions(BarcodeImageOptions options, Barcoder.IBarcode barcode)
            {
                Margin = options.Margin ?? barcode.Margin;
                IncludeEanAsText = options.IncludeEanAsText;
                Scale = options.Scale;
                BarHeightFor1DCode = options.BarHeightFor1DCode;
                EanFontFamily = options.EanFontFamily ?? "Arial";
                BackColor = ImagingHelper.ConvertColor(options.BackColor);
                ForeColor = ImagingHelper.ConvertColor(options.ForeColor);
                TextColor = ImagingHelper.ConvertColor(options.TextColor);
            }

            public int Margin { get; }
            public bool IncludeEanAsText { get; }
            public int Scale { get; }
            public int BarHeightFor1DCode { get; }
            public string EanFontFamily { get; }
            public SharpColor BackColor { get; }
            public SharpColor ForeColor { get; }
            public SharpColor TextColor { get; }
        }

        #endregion

        #region EanTextRenderer

        static class EanTextRenderer
        {
            private const int UnscaledFontSize = 9;
            private const int ContentMargin = 9;
            private const int ContentVerticalOffset = 0;

            public static void Render(Image<Rgba32> image, Barcoder.IBarcode barcode, ImageOptions o)
            {
                var font = SystemFonts.CreateFont(o.EanFontFamily, UnscaledFontSize * o.Scale, FontStyle.Regular);

                switch (barcode.Metadata.CodeKind)
                {
                    case Barcoder.BarcodeType.EAN8:
                        RenderContentForEan8(image, barcode.Content, font, o);
                        break;
                    case Barcoder.BarcodeType.EAN13:
                        RenderContentForEan13(image, barcode.Content, font, o);
                        break;
                }
            }

            private static void RenderContentForEan8(Image<Rgba32> image, string content, Font font, ImageOptions o)
            {
                int ApplyScale(int value) => value * o.Scale;

                var color = o.BackColor.WithAlpha(1);

                RenderTextBack(image, ApplyScale(o.Margin + 3), image.Height - ApplyScale(o.Margin + ContentMargin), ApplyScale(29), ApplyScale(ContentMargin), color);
                RenderTextBack(image, ApplyScale(o.Margin + 35), image.Height - ApplyScale(o.Margin + ContentMargin), ApplyScale(29), ApplyScale(ContentMargin), color);

                float textTop = image.Height - (o.Margin + ContentMargin / 2.0f - ContentVerticalOffset) * o.Scale;
                float textCenter1 = (29.0f / 2.0f + o.Margin + 3.0f) * o.Scale;
                float textCenter2 = (29.0f / 2.0f + o.Margin + 35.0f) * o.Scale;
                RenderText(image, content.Substring(0, 4), textCenter1, textTop, font, o.TextColor);
                RenderText(image, content.Substring(4), textCenter2, textTop, font, o.TextColor);
            }

            private static void RenderContentForEan13(Image<Rgba32> image, string content, Font font, ImageOptions o)
            {
                var color = o.BackColor.WithAlpha(1);

                int ApplyScale(int value) => value * o.Scale;
                RenderTextBack(image, ApplyScale(o.Margin + 3), image.Height - ApplyScale(o.Margin + ContentMargin), ApplyScale(43), ApplyScale(ContentMargin), color);
                RenderTextBack(image, ApplyScale(o.Margin + 49), image.Height - ApplyScale(o.Margin + ContentMargin), ApplyScale(43), ApplyScale(ContentMargin), color);

                float textTop = image.Height - (o.Margin + ContentMargin / 2.0f - ContentVerticalOffset) * o.Scale;
                float textCenter1 = (o.Margin - 4.0f) * o.Scale;
                float textCenter2 = (43.0f / 2.0f + o.Margin + 3.0f) * o.Scale;
                float textCenter3 = (43.0f / 2.0f + o.Margin + 49.0f) * o.Scale;
                RenderText(image, content[..1], textCenter1, textTop, font, o.TextColor);
                RenderText(image, content.Substring(1, 6), textCenter2, textTop, font, o.TextColor);
                RenderText(image, content[7..], textCenter3, textTop, font, o.TextColor);
            }

            private static void RenderTextBack(Image<Rgba32> image, int x, int y, int width, int height, Color color)
            {
                image.Mutate(ctx => ctx.FillPolygon(
                    color,
                    new Vector2(x, y),
                    new Vector2(x + width, y),
                    new Vector2(x + width, y + height),
                    new Vector2(x, y + height)));
            }

            private static void RenderText(Image<Rgba32> image, string text, float x, float y, Font font, Color color)
            {
                var options = new RichTextOptions(font)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Origin = new PointF(x, y),
                };

                image.Mutate(ctx => ctx.DrawText(options, text, color));
            }
        }

        #endregion
    }
}
