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

        public IActionResult Svg(string data, BarcodeType type)
        {
            var code = _encoder.EncodeBarcode(data, type);
            var svg = code.GenerateSvg(new BarcodeSvgOptions
            {
                IncludeEanAsText = true,
                Margin = 0
            });

            return Content(svg, "image/svg+xml");
        }

        public async Task<IActionResult> Image(string data, BarcodeType type)
        {
            var code = _encoder.EncodeBarcode(data, type);
            using var image = code.GenerateImage(new BarcodeImageOptions 
            {
                IncludeEanAsText = true,
                Margin = 0
            });

            var stream = new MemoryStream();
            await image.SaveAsync(stream);
            stream.Position = 0;

            return File(stream, "image/png");
        }
    }
}
