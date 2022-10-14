using System.Drawing;
using Barcoder.Renderer.Image;
using Barcoder.Renderer.Svg;

namespace Smartstore.Imaging.Barcodes.Impl
{
    internal class DefaultBarcode : IBarcode
    {
        private readonly Barcoder.IBarcode _code;

        public DefaultBarcode(Barcoder.IBarcode code, BarcodePayload payload)
        {
            _code = code;
            Payload = payload;
        }

        public BarcodePayload Payload { get; }

        public string GenerateSvg(BarcodeSvgOptions options = null)
        {
            options ??= new BarcodeSvgOptions();
            
            var renderer = new SvgRenderer(includeEanContentAsText: options.IncludeEanAsText);

            using var stream = new MemoryStream();
            using var reader = new StreamReader(stream);

            renderer.Render(_code, stream);
            stream.Position = 0;

            string svg = reader.ReadToEnd();
            return svg;
        }

        public IImage GenerateImage(BarcodeImageOptions options = null)
        {
            var generator = new ImageGenerator(options ?? new BarcodeImageOptions());
            return generator.GenerateImage(_code);
        }
    }
}
