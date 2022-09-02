using Net.Codecrete.QrCodeGenerator;

namespace Smartstore.Imaging.QRCodes
{
    public class DefaultQRCodeEncoder : IQRCodeEncoder
    {
        public IQRCode EncodeQRCode(QRPayload payload, EccLevel eccLevel)
        {
            if (payload == null)
            {
                return null;
            }
            
            var serializedPayload = payload.Serialize();

            if (!serializedPayload.HasValue())
            {
                return null;
            }

            var qrCodeEncoded = QrCode.EncodeText(serializedPayload, TranslateErrorCorrectionLevel(eccLevel));
            var qrCode = new DefaultQRcode(qrCodeEncoded, serializedPayload);

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
        public static IQRCode EncodeQRCode(this DefaultQRCodeEncoder encoder, string payload, EccLevel eccLevel)
        {
            return encoder.EncodeQRCode(new RawQRPayload(payload), eccLevel);
        }
    }
}