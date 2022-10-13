namespace Smartstore.Imaging.QrCodes
{
    /// <summary>
    /// Encodes QR code payloads.
    /// </summary>
    public interface IQrCodeEncoder
    {
        /// <summary>
        /// Serializes a QR payload and encodes a QR code.
        /// </summary>
        /// <param name="payload">The payload to encode.</param>
        /// <param name="eccLevel">The error correction level to apply while encoding <see cref="EccLevel"/>.</param>
        /// <returns>The encoded QR code</returns>
        IQrCode EncodeQrCode(QrPayload payload, EccLevel eccLevel);
    }

    public static class IQrCodeEncoderExtensions
    {
        /// <summary>
        /// Encodes a raw QR code payload.
        /// </summary>
        /// <param name="payload">The payload to encode.</param>
        /// <param name="eccLevel">The error correction level to apply while encoding <see cref="EccLevel"/>.</param>
        /// <returns>The encoded QR code</returns>
        public static IQrCode EncodeQrCode(this IQrCodeEncoder encoder, string payload, EccLevel eccLevel)
        {
            return encoder.EncodeQrCode(new RawQrPayload(payload), eccLevel);
        }
    }
}