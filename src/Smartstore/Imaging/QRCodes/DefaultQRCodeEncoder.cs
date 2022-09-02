using Net.Codecrete.QrCodeGenerator;

namespace Smartstore.Imaging.QRCodes
{
    internal class DefaultQRCodeEncoder : IQRCodeEncoder
    {
        public IQRCode EncodeQRCode(QRPayload payload, EccLevel eccLevel)
        {
            Guard.NotNull(payload, nameof(payload));

            var qrCodeEncoded = QrCode.EncodeText(payload.Serialize(), TranslateErrorCorrectionLevel(eccLevel));
            var qrCode = new DefaultQRcode(qrCodeEncoded, payload);

            return qrCode;
        }

        internal static QrCode.Ecc TranslateErrorCorrectionLevel(EccLevel eccLevel)
        {
            switch (eccLevel) 
            {
                case EccLevel.Low:
                    return QrCode.Ecc.Low;
                case EccLevel.Medium:
                    return QrCode.Ecc.Medium;
                case EccLevel.Quartile:
                    return QrCode.Ecc.Quartile;
                case EccLevel.High:
                    return QrCode.Ecc.High;
                default: 
                    return QrCode.Ecc.Medium;
            }
        }
    }

    public static class QRCodeEncoderExtensions
    {
        /// <summary>
        /// Encodes a raw QR code payload.
        /// </summary>
        /// <param name="payload">The payload to encode.</param>
        /// <param name="eccLevel">The error correction level to apply while encoding <see cref="EccLevel"/>.</param>
        /// <returns>The encoded QR code</returns>
        public static IQRCode EncodeQRCode(this DefaultQRCodeEncoder encoder, string payload, EccLevel eccLevel)
        {
            return encoder.EncodeQRCode(new RawQRPayload(payload), eccLevel);
        }
    }
}