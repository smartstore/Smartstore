namespace Smartstore.PayPal.Client.Messages
{
    public class AccessToken
    {
        private readonly DateTime _createDate;

        public AccessToken()
        {
            _createDate = DateTime.Now;
        }

        [JsonProperty("access_token")]
        public string Token;

        [JsonProperty("token_type")]
        public string TokenType;

        [JsonProperty("expires_in")]
        public int ExpiresIn;

        public bool IsExpired()
        {
            var expireDate = _createDate.Add(TimeSpan.FromSeconds(ExpiresIn));
            return DateTime.Now.CompareTo(expireDate) > 0;
        }
    }
}
