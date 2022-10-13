using System.Drawing;
using Net.Codecrete.QrCodeGenerator;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Smartstore.Imaging.Adapters.ImageSharp;
using SharpColor = SixLabors.ImageSharp.Color;
using SharpRectangle = SixLabors.ImageSharp.Rectangle;
using SharpImageFormat = SixLabors.ImageSharp.Formats.IImageFormat;

namespace Smartstore.Imaging.QRCodes
{
    internal class DefaultQRcode : IQRCode
    {
        private readonly static SharpImageFormat PngFormat = SharpImageFactory.FindInternalImageFormat("png");
        private readonly QrCode _code;
        
        public DefaultQRcode(QrCode code, QRPayload payload)
        {
            _code = code;
            Payload = payload;
        }

        public QRPayload Payload { get; }

        public string GenerateSvg(string foreColor = "#000", string backColor = "#fff", int border = 3)
        {
            Guard.NotEmpty(foreColor, nameof(foreColor));
            Guard.NotEmpty(backColor, nameof(backColor));
            Guard.NotNegative(border, nameof(border));

            return _code.ToSvgString(border, foreColor, backColor);
        }

        public IImage GenerateImage(Color foreColor, Color backColor, int scale = 3, int border = 3)
        {
            Guard.NotNegative(scale, nameof(scale));
            Guard.NotNegative(border, nameof(border));

            int size = _code.Size;
            int dim = (size + border * 2) * scale;

            if (dim > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), "Scale or border too large");
            }

            // Create bitmap.
            SixLabors.ImageSharp.Image<Rgb24> image = new SixLabors.ImageSharp.Image<Rgb24>(dim, dim);

            image.Mutate(img =>
            {
                var background = SharpColor.FromRgba(backColor.R, backColor.G, backColor.B, backColor.A);
                var foreground = SharpColor.FromRgba(foreColor.R, foreColor.G, foreColor.B, foreColor.A);

                // Draw background.
                img.Fill(background);
                
                // Draw modules.
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        if (_code.GetModule(x, y))
                        {
                            img.Fill(foreground, new SharpRectangle((x + border) * scale, (y + border) * scale, scale, scale));
                        }
                    }
                }
            });

            return new SharpImage(image, PngFormat);
        }
    }
}