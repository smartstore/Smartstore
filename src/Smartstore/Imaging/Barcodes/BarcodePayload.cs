namespace Smartstore.Imaging.Barcodes
{
    /// <summary>
    /// Represents barcode payload.
    /// </summary>
    public class BarcodePayload
    {
        /// <summary>
        /// The string data to encode.
        /// </summary>
        public string Data { get; init; }

        /// <summary>
        /// The barcode type to generate.
        /// </summary>
        public BarcodeType Type { get; init; }

        /// <summary>
        /// Whether the current barcode type is any EAN type.
        /// </summary>
        public bool IsEan
        {
            get => Type is (BarcodeType.EAN8 or BarcodeType.EAN13);
        }
    }
}
