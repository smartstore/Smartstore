namespace Smartstore.Web.Api.Models
{
    public class WebApiUser
    {
        private DateTime? _lastRequest;

        /// <summary>
        /// The identifier of the generic attribute where the user's access data is stored.
        /// </summary>
        public int GenericAttributeId { get; set; }

        /// <summary>
        /// Customer identifier.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// A value indicating whether the user has access to the API.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The user's public access key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// The user's secret access key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// The date on which the last API request took place (in UTC).
        /// </summary>
        public DateTime? LastRequest
        {
            get => _lastRequest;
            set => _lastRequest = value;
        }

        /// <summary>
        /// A value indicating whether the user data is valid.
        /// </summary>
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
