namespace Smartstore.Imaging.QrCodes
{
    /// <summary>
    /// A payload implementation that takes a raw encoded string.
    /// </summary>
    public class RawQrPayload : QrPayload
    {
        public RawQrPayload(string payload)
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