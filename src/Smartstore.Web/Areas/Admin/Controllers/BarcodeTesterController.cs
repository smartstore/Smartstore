using System.Drawing;
using Smartstore.Imaging;
using Smartstore.Imaging.Barcodes;

namespace Smartstore.Admin.Controllers
{
    public class BarcodeTesterController : AdminController
    {
        private readonly IBarcodeEncoder _encoder;

        public BarcodeTesterController(IBarcodeEncoder encoder)
        {
            _encoder = encoder;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Svg(
            string data,
            BarcodeType type,
            int? m,             /* Margin */
            string bgc = null,  /* BackColor */
            string fc = null,   /* ForeColor */
            string tc = null,   /* TextColor */
            bool? ean = false,   /* IncludeEanText */
            string font = null  /* EanFont */)
        {
            try
            {
                var code = _encoder.EncodeBarcode(data, type);

                var options = new BarcodeSvgOptions
                {
                    Margin = m,
                    BackColor = bgc ?? "#fff",
                    ForeColor = fc ?? "#000",
                    TextColor = tc ?? "#000",
                    IncludeEanAsText = ean ?? false,
                    EanFontFamily = font
                };

                var svg = code.GenerateSvg(options);

                return Content(svg, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> Image(
            string data, 
            BarcodeType type,
            int? scale,         /* PixelSize */
            int? m,             /* Margin */
            string bgc = null,  /* BackColor */
            string fc = null,   /* ForeColor */
            string tc = null,   /* TextColor */
            bool? ean = false,  /* IncludeEanText */
            string font = null, /* EanFont */
            int? h = null       /* 1DBarHeight */)
        {
            try
            {
                var code = _encoder.EncodeBarcode(data, type);

                if (!ImagingHelper.TryTranslateColor(bgc, out var backColor))
                {
                    backColor = Color.White;
                }

                if (!ImagingHelper.TryTranslateColor(fc, out var foreColor))
                {
                    foreColor = Color.Black;
                }

                if (!ImagingHelper.TryTranslateColor(tc, out var textColor))
                {
                    textColor = Color.Black;
                }

                var options = new BarcodeImageOptions
                {
                    Scale = scale ?? 3,
                    Margin = m,
                    BackColor = backColor,
                    ForeColor = foreColor,
                    TextColor = textColor,
                    IncludeEanAsText = ean ?? false,
                    EanFontFamily = font,
                    BarHeightFor1DCode = h ?? 40
                };

                using var image = code.GenerateImage(options);

                var stream = new MemoryStream();
                await image.SaveAsync(stream);
                stream.Position = 0;

                return File(stream, "image/png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
