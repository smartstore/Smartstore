using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    public class AccessTokenRequest : PayPalRequest
    {
        public AccessTokenRequest(
            string clientId = null,
            string secret = null,
            string refreshToken = null,
            string authCode = null,
            string sharedId = null,
            string sellerNonce = null)
            : base("/v1/oauth2/token", HttpMethod.Post, typeof(AccessToken))
        {
            if (clientId.HasValue() && secret.HasValue())
            {
                var authorizationString = Convert.ToBase64String($"{clientId}:{secret}".GetBytes());
                Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationString);
            }

            var body = new Dictionary<string, string>()
            {
                {"grant_type", "client_credentials"}
            };

            if (refreshToken.HasValue())
            {
                body["grant_type"] = "refresh_token";
                body.Add("refresh_token", refreshToken);
            }

            // 
            if (sharedId.HasValue() && authCode.HasValue() && sellerNonce.HasValue())
            {
                var authorizationString = Convert.ToBase64String($"{sharedId}:".GetBytes());
                Headers.Authorization = new AuthenticationHeaderValue("Basic", authorizationString);

                body["grant_type"] = "authorization_code";
                body.Add("code", authCode);
                body.Add("code_verifier", sellerNonce);
            }

            Body = body;

            ContentType = "application/x-www-form-urlencoded";
        }
    }
}