namespace Smartstore.Imaging.Barcodes
{
    /// <summary>
    /// Encodes barcode payloads.
    /// </summary>
    public interface IBarcodeEncoder
    {
        /// <summary>
        /// Encodes a barcode payload.
        /// </summary>
        /// <param name="content">The barcode payload/data to encode.</param>
        /// <param name="type">Type of barcode to encode.</param>
        /// <returns>The encoded barcode</returns>
        IBarcode EncodeBarcode(string data, BarcodeType type);
    }
}
