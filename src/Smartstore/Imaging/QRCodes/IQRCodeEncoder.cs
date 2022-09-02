namespace Smartstore.Imaging.QRCodes
{
    /// <summary>
    /// An interface for QR code encoders.
    /// </summary>
    public interface IQRCodeEncoder
    {
        /// <summary>
        /// Serializes a payload and encodes a QR code.
        /// </summary>
        /// <param name="payload">The payload to encode <see cref="QRPayload"/>.</param>
        /// <param name="eccLevel">The error correction level to apply while encoding <see cref="EccLevel"/>.</param>
        /// <returns>The encoded QR code.</returns>
        IQRCode EncodeQRCode(QRPayload payload, EccLevel eccLevel);
    }
}