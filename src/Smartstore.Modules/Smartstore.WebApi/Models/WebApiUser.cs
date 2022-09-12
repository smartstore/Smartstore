namespace Smartstore.WebApi.Models
{
    public class WebApiUser
    {
        public int GenericAttributeId { get; set; }
        public int CustomerId { get; set; }
        public bool Enabled { get; set; }
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
        public DateTime? LastRequest { get; set; }

        public bool IsValid 
            => GenericAttributeId != 0 && CustomerId != 0 && !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(SecretKey);

        public override string ToString()
        {
            // INFO: the data that is stored as generic attribute.
            if (!string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(SecretKey))
            {
                return LastRequest.HasValue
                    ? string.Join('¶', Enabled, PublicKey, SecretKey, LastRequest.Value.ToString("o"))
                    : string.Join('¶', Enabled, PublicKey, SecretKey);
            }

            return string.Empty;
        }
    }
}
