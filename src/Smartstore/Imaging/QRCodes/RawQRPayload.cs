namespace Smartstore.Imaging.QRCodes
{
    public class RawQRPayload : QRPayload
    {
        public RawQRPayload(string payload)
        {
            Payload = payload;
        }

        public string Payload { get; set; }

        public override string Serialize()
        {
            return Payload;
        }
    }
}