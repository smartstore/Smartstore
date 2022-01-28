namespace Smartstore.PayPal.Client.Messages
{
    public class AccessToken
    {
        [JsonProperty("access_token")]
        public string Token;

        [JsonProperty("token_type")]
        public string TokenType;

        [JsonProperty("expires_in")]
        public int ExpiresIn;

        private DateTime createDate;

        public AccessToken()
        {
            createDate = DateTime.Now;
        }

        public bool IsExpired()
        {
            DateTime expireDate = createDate.Add(TimeSpan.FromSeconds(ExpiresIn));
            return DateTime.Now.CompareTo(expireDate) > 0;
        }
    }
}
