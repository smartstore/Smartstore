using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    public class AccessTokenRequest : PayPalRequest
    {
        public AccessTokenRequest(string clientId, string secret, string refreshToken = null) 
            : base("/v1/oauth2/token", HttpMethod.Post, typeof(AccessToken))
        {
            var authorizationString = Convert.ToBase64String($"{clientId}:{secret}".GetBytes());
            Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationString);

            var body = new Dictionary<string, string>()
            {
                {"grant_type", "client_credentials"}
            };

            if (refreshToken.HasValue())
            {
                body["grant_type"] = "refresh_token";
                body.Add("refresh_token", refreshToken);
            }

            Body = body;

            ContentType = "application/x-www-form-urlencoded";
        }
    }
}