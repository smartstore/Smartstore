namespace Smartstore.Imaging.Barcodes
{
    /// <summary>
    /// Abstract base class for QR code payloads.
    /// </summary>
    public abstract class QrPayload : BarcodePayload
    {
        protected QrPayload()
            : base("qr", BarcodeType.Qr)
        {
        }

        public override string Data => Serialize();

        /// <summary>
        /// Serializes the payload object.
        /// </summary>
        /// <returns>Payload as string.</returns>
        public abstract string Serialize();
    }
}
