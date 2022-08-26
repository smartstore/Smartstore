namespace Smartstore.Imaging.QRCodes
{
    public interface IQRCodeEncoder
    {
        IQRCode EncodeQRCode(QRPayload payload, EccLevel eccLevel);
    }
}
