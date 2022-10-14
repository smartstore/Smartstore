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
        /// <param name="payload">The barcode payload to encode.</param>
        /// <returns>The encoded barcode</returns>
        IBarcode EncodeBarcode(BarcodePayload payload);
    }

    public static class IBarcodeEncoderExtensions
    {
        /// <summary>
        /// Encodes a barcode payload.
        /// </summary>
        /// <param name="content">The barcode data to encode.</param>
        /// <param name="type">Type of barcode to encode.</param>
        /// <returns>The encoded barcode</returns>
        public static IBarcode EncodeBarcode(this IBarcodeEncoder encoder, string data, BarcodeType type)
        {
            return encoder.EncodeBarcode(new BarcodePayload(data, type));
        }
    }
}
