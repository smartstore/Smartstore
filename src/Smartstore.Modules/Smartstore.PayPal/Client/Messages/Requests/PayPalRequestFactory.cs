#nullable enable

using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    // TODO: (mh) Continue this class implementation. Register in DI and refactor all callers. Delete obsolete request classes.
    public partial class PayPalRequestFactory
    {
        private static string FormatPath(string path, params string[] tokens)
            => PayPalRequest.FormatPath(path, tokens);

        #region Identity

        public PayPalRequest GenerateClientToken()
            => new("/v1/identity/generate-token", HttpMethod.Post);

        public PayPalRequest AccessToken(
            string? clientId = null,
            string? secret = null,
            string? refreshToken = null,
            string? authCode = null,
            string? sharedId = null,
            string? sellerNonce = null)
            => new AccessTokenRequest(clientId, secret, refreshToken, authCode, sharedId, sellerNonce);

        #endregion

        #region Merchant

        public PayPalRequest GetMerchantStatus(string partnerId, string payerId)
            => new(FormatPath("/v1/customer/partners/{0}/merchant-integrations/{1}", partnerId, payerId), HttpMethod.Get);

        public PayPalRequest GetSellerCredentials(string partnerId, string token)
        {
            var request = new PayPalRequest(FormatPath("/v1/customer/partners/{0}/merchant-integrations/credentials", partnerId), HttpMethod.Get);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        #endregion

        #region Notification

        public PayPalRequest CreateWebhook()
            => new("/v1/notifications/webhooks", HttpMethod.Post);

        public PayPalRequest ListWebhooks()
            => new("/v1/notifications/webhooks", HttpMethod.Get);

        #endregion

    }
}
