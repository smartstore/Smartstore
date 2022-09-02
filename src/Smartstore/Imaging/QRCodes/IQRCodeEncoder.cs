namespace Smartstore.Imaging.QRCodes
{
    /// <summary>
    /// Encodes QR code payloads.
    /// </summary>
    public interface IQRCodeEncoder
    {
        /// <summary>
        /// Serializes a QR payload and encodes a QR code.
        /// </summary>
        /// <param name="payload">The payload to encode.</param>
        /// <param name="eccLevel">The error correction level to apply while encoding <see cref="EccLevel"/>.</param>
        /// <returns>The encoded QR code</returns>
        IQRCode EncodeQRCode(QRPayload payload, EccLevel eccLevel);
    }

    public static class IQRCodeEncoderExtensions
    {
        /// <summary>
        /// Encodes a raw QR code payload.
        /// </summary>
        /// <param name="payload">The payload to encode.</param>
        /// <param name="eccLevel">The error correction level to apply while encoding <see cref="EccLevel"/>.</param>
        /// <returns>The encoded QR code</returns>
        public static IQRCode EncodeQRCode(this IQRCodeEncoder encoder, string payload, EccLevel eccLevel)
        {
            return encoder.EncodeQRCode(new RawQRPayload(payload), eccLevel);
        }
    }
}