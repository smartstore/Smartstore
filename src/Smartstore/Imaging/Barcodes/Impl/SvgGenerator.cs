using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Barcoder.Renderers;
using SvgLib;

namespace Smartstore.Imaging.Barcodes.Impl
{
    internal sealed class SvgGenerator
    {
        private static readonly int[] Ean8LongerBars = new[] { 0, 2, 32, 34, 64, 66 };
        private static readonly int[] Ean13LongerBars = new[] { 0, 2, 46, 48, 92, 94 };

        private readonly BarcodeSvgOptions _options;

        public SvgGenerator(BarcodeSvgOptions options)
        {
            _options = Guard.NotNull(options, nameof(options));

            if (options.Margin.HasValue)
            {
                Guard.NotNegative(options.Margin.Value, nameof(options.Margin));
            }
        }

        private bool IncludeEanContent(Barcoder.IBarcode barcode)
        {
            return _options.IncludeEanAsText && barcode.IsEanBarcode();
        }

        public string GenerateSvg(Barcoder.IBarcode barcode)
        {
            Guard.NotNull(barcode, nameof(barcode));

            // Clone and fix options
            var opts = new SvgOptions(_options, barcode);

            var currentCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                if (barcode.Metadata.CodeKind == Barcoder.BarcodeType.QR)
                {
                    return RenderQR(barcode, opts);
                }
                else if (barcode.Bounds.Y == 1)
                {
                    return Render1D(barcode, opts);
                }
                else if (barcode.Bounds.Y > 1)
                {
                    return Render2D(barcode, opts);
                }
                else
                {
                    throw new NotSupportedException($"Y value of {barcode.Bounds.Y} is invalid");
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = currentCulture;
            }
        }

        private string Render1D(Barcoder.IBarcode barcode, SvgOptions o)
        {
            var width = barcode.Bounds.X + 2 * o.Margin;
            var height = (IncludeEanContent(barcode) ? 55 : 50) + (o.Margin * 2);

            var sb = new StringBuilder()
                .Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n")
                .Append("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n")
                .Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"0 0 {width} {height}\">\n")
                .Append($"\t<rect width=\"100%\" height=\"100%\" fill=\"{o.BackColor}\"/>\n")
                .Append($"\t<svg x=\"{o.Margin}\" y=\"{o.Margin}\" height=\"{height - (o.Margin * 2)}\">\n")
                .Append($"\t<g stroke=\"{o.ForeColor}\" stroke-width=\"1\" stroke-linecap=\"butt\">\n");

            var prevBar = false;
            for (var x = 0; x < barcode.Bounds.X; x++)
            {
                if (!barcode.At(x, 0))
                {
                    prevBar = false;
                    continue;
                }

                int lineHeight = height;
                if (IncludeEanContent(barcode))
                {
                    if (barcode.Metadata.CodeKind == Barcoder.BarcodeType.EAN13)
                    {
                        if (!Ean13LongerBars.Contains(x))
                        {
                            lineHeight = 48;
                        }
                    }
                    else
                    {
                        if (!Ean8LongerBars.Contains(x))
                        {
                            lineHeight = 48;
                        }
                    }
                }

                if (prevBar)
                {
                    var x1 = x - 0.25;
                    sb.Append($"\t<line stroke-width=\"1.5\" x1=\"{x1}\" x2=\"{x1}\" y1=\"0\" y2=\"{lineHeight}\" />\n");
                }
                else
                {
                    var x1 = x;
                    sb.Append($"\t<line x1=\"{x1}\" x2=\"{x1}\" y1=\"0\" y2=\"{lineHeight}\" />\n");
                }

                prevBar = true;
            }

            sb.Append($"\t</g></svg>\n");

            if (IncludeEanContent(barcode))
            {
                if (barcode.Metadata.CodeKind == Barcoder.BarcodeType.EAN13)
                {
                    AddText(sb, 4, 54.5D, barcode.Content[..1], o);
                    AddText(sb, 21, 54.5D, barcode.Content.Substring(1, 6), o);
                    AddText(sb, 67, 54.5D, barcode.Content[7..], o);
                }
                else
                {
                    AddText(sb, 18, 54.5D, barcode.Content[..4], o);
                    AddText(sb, 50, 54.5D, barcode.Content[4..], o);
                }
            }

            return sb
                .Append("</svg>\n")
                .ToString();
        }

        private static void AddText(StringBuilder sb, double x, double y, string t, SvgOptions o)
        {
            sb.Append($"\t<text style=\"font-family: Arial\" font-size=\"8\" x=\"{x + o.Margin - 10}\" y=\"{y + o.Margin}\" stroke-width=\"0\" fill=\"{o.TextColor}\">{t}</text>\n");
        }

        private static string Render2D(Barcoder.IBarcode barcode, SvgOptions o)
        {
            var document = SvgDocument.Create();
            document.ViewBox = new SvgViewBox
            {
                Left = 0,
                Top = 0,
                Width = barcode.Bounds.X + 2 * o.Margin,
                Height = barcode.Bounds.Y + 2 * o.Margin
            };
            document.Fill = o.BackColor;
            document.Stroke = o.ForeColor;
            document.StrokeWidth = .05;
            document.StrokeLineCap = SvgStrokeLineCap.Butt;

            SvgGroup group = document.AddGroup();
            group.Fill = o.ForeColor;
            for (int y = 0; y < barcode.Bounds.Y; y++)
            {
                for (int x = 0; x < barcode.Bounds.X; x++)
                {
                    if (barcode.At(x, y))
                    {
                        SvgRect rect = group.AddRect();
                        rect.X = x + o.Margin;
                        rect.Y = y + o.Margin;
                        rect.Width = 1;
                        rect.Height = 1;
                    }
                }
            }

            return ToSvg(document);
        }

        #region QR code

        private static string RenderQR(Barcoder.IBarcode barcode, SvgOptions o)
        {
            var size = barcode.Bounds.X; // barcode.Metadata.Dimensions;
            var dim = size + o.Margin * 2;

            var sb = new StringBuilder()
                .Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n")
                .Append("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n")
                .Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"0 0 {dim} {dim}\" stroke=\"none\">\n")
                .Append($"\t<rect width=\"100%\" height=\"100%\" fill=\"{o.BackColor}\"/>\n")
                .Append("\t<path d=\"");

            // Work on copy as it is destructive
            var modules = CopyModules(barcode, size);
            CreatePath(sb, modules, o.Margin);

            return sb
                .Append($"\" fill=\"{o.ForeColor}\"/>\n")
                .Append("</svg>\n")
                .ToString();
        }

        private static bool[,] CopyModules(Barcoder.IBarcode barcode, int size)
        {
            var modules = new bool[size, size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    modules[y, x] = barcode.At(x, y);
                }
            }

            return modules;
        }

        /// <summary>
        /// Append a SVG/XAML path for the QR code to the provided string builder
        /// </summary>
        private static void CreatePath(StringBuilder sb, bool[,] modules, int margin)
        {
            // Simple algorithms to reduce the number of rectangles for drawing the QR code
            // and reduce SVG/XAML size.
            var size = modules.GetLength(0);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (modules[y, x])
                    {
                        DrawLargestRectangle(sb, modules, x, y, margin);
                    }
                }
            }
        }

        /// <summary>
        /// Find, draw and clear largest rectangle with (x, y) as the top left corner
        /// </summary>
        private static void DrawLargestRectangle(StringBuilder sb, bool[,] modules, int x, int y, int margin)
        {
            var size = modules.GetLength(0);

            var bestW = 1;
            var bestH = 1;
            var maxArea = 1;

            var xLimit = size;
            var iy = y;
            while (iy < size && modules[iy, x])
            {
                var w = 0;
                while (x + w < xLimit && modules[iy, x + w])
                {
                    w++;
                }

                var area = w * (iy - y + 1);
                if (area > maxArea)
                {
                    maxArea = area;
                    bestW = w;
                    bestH = iy - y + 1;
                }
                xLimit = x + w;
                iy++;
            }

            // append path command
            if (x != 0 || y != 0)
            {
                sb.Append(' ');
            }
            sb.Append($"M{x + margin},{y + margin}h{bestW}v{bestH}h{-bestW}z");

            // clear processed modules
            ClearRectangle(modules, x, y, bestW, bestH);
        }

        /// <summary>
        /// Clear a rectangle of modules
        /// </summary>
        private static void ClearRectangle(bool[,] modules, int x, int y, int width, int height)
        {
            for (var iy = y; iy < y + height; iy++)
            {
                for (var ix = x; ix < x + width; ix++)
                {
                    modules[iy, ix] = false;
                }
            }
        }

        #endregion

        private static string ToSvg(SvgDocument doc)
        {
            using var stream = new MemoryStream();
            using var reader = new StreamReader(stream);

            doc.Save(stream);
            stream.Position = 0;

            return reader.ReadToEnd();
        }

        #region ImageOptions

        readonly struct SvgOptions
        {
            public SvgOptions(BarcodeSvgOptions options, Barcoder.IBarcode barcode)
            {
                Margin = options.Margin ?? barcode.Margin;
                IncludeEanAsText = options.IncludeEanAsText;
                BackColor = options.BackColor.NullEmpty() ?? "#fff";
                ForeColor = options.ForeColor ?? "#000";
                TextColor = options.TextColor ?? "#000";
            }

            public int Margin { get; }
            public bool IncludeEanAsText { get; }
            public string BackColor { get; }
            public string ForeColor { get; }
            public string TextColor { get; }
        }

        #endregion
    }
}