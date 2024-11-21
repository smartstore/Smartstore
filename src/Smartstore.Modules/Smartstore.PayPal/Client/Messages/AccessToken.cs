namespace Smartstore.PayPal.Client.Messages
{
    public class AccessToken
    {
        private readonly DateTime _createDate;

        public AccessToken()
        {
            _createDate = DateTime.Now;
        }

        [JsonProperty("access_token", DefaultValueHandling = DefaultValueHandling.Include)]
        public string Token;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string TokenType;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int ExpiresIn;

        public bool IsExpired()
        {
            var expireDate = _createDate.Add(TimeSpan.FromSeconds(ExpiresIn));
            return DateTime.Now.CompareTo(expireDate) > 0;
        }
    }
}
