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
            var generator = new SvgGenerator(options ?? new BarcodeSvgOptions());
            return generator.GenerateSvg(_code);
        }

        public IImage GenerateImage(BarcodeImageOptions options = null)
        {
            var generator = new ImageGenerator(options ?? new BarcodeImageOptions());
            return generator.GenerateImage(_code);
        }
    }

    internal static class BarcodeExtensions
    {
        public static bool IsEanBarcode(this Barcoder.IBarcode barcode)
            => barcode?.Metadata.CodeKind == Barcoder.BarcodeType.EAN8 || barcode?.Metadata.CodeKind == Barcoder.BarcodeType.EAN13;
    }
}
