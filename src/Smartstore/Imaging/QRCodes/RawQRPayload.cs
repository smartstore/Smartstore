namespace Smartstore.Imaging.QRCodes
{
    /// <summary>
    /// TODO: Describe
    /// </summary>
    public class RawQRPayload : QRPayload
    {
        public RawQRPayload(string payload)
        {
            Guard.NotEmpty(payload, nameof(payload));
            
            Payload = payload;
        }

        public string Payload { get; }

        public override string Serialize()
        {
            return Payload;
        }
    }
}