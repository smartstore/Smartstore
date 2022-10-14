namespace Smartstore.Imaging.Barcodes.Impl
{
    internal static class BarcodeExtensions
    {
        public static bool IsEanBarcode(this Barcoder.IBarcode barcode)
            => barcode?.Metadata.CodeKind == Barcoder.BarcodeType.EAN8 || barcode?.Metadata.CodeKind == Barcoder.BarcodeType.EAN13;
    }
}
