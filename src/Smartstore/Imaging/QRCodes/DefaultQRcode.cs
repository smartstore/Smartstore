using System.Drawing;
using Net.Codecrete.QrCodeGenerator;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Smartstore.Imaging.Adapters.ImageSharp;
using SharpColor = SixLabors.ImageSharp.Color;
using SharpRectangle = SixLabors.ImageSharp.Rectangle;

namespace Smartstore.Imaging.QRCodes
{
    public class DefaultQRcode : IQRCode
    {
        private readonly QrCode _qrCode;
        
        public DefaultQRcode(QrCode qrCode, string payload)
        {
            _qrCode = qrCode;
            Payload = payload;
        }

        public string Payload { get; set; }

        public IImage GenerateImage(Color foreColor, Color backColor, int scale, int border)
        {
            Guard.NotNegative(scale, nameof(scale));
            Guard.NotNegative(border, nameof(border));

            int size = _qrCode.Size;
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
                        if (_qrCode.GetModule(x, y))
                        {
                            img.Fill(foreground, new SharpRectangle((x + border) * scale, (y + border) * scale, scale, scale));
                        }
                    }
                }
            });

            return new SharpImage(image, SharpImageFactory.FindInternalImageFormat("png"));
        }

        public string GenerateSvg(int border = 3, string foreColor = "#000", string backColor = "#fff")
        {
            return _qrCode.ToSvgString(border, foreColor, backColor);
        }
    }
}