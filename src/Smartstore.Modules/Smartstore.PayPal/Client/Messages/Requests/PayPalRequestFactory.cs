#nullable enable

using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    public partial class PayPalRequestFactory
    {
        private static string FormatPath(string path, params string[] tokens)
            => PayPalRequest.FormatPath(path, tokens);

        #region Identity

        /// <summary>
        /// Generates a client token (mandatory for presenting credit card processing in hosted fields).
        /// </summary>
        public PayPalRequest GenerateClientToken()
            => new("/v1/identity/generate-token", HttpMethod.Post, typeof(object));

        public PayPalRequest<AccessToken> AccessToken(
            string? clientId = null,
            string? secret = null,
            string? refreshToken = null,
            string? authCode = null,
            string? sharedId = null,
            string? sellerNonce = null)
            => new AccessTokenRequest(clientId, secret, refreshToken, authCode, sharedId, sellerNonce);

        #endregion

        #region Merchant

        /// <summary>
        /// Gets merchant status.
        /// </summary>
        public PayPalRequest<MerchantStatus> MerchantStatusGet(string partnerId, string payerId)
            => new(FormatPath("/v1/customer/partners/{0}/merchant-integrations/{1}", partnerId, payerId), HttpMethod.Get);

        /// <summary>
        /// Gets seller credentials.
        /// </summary>
        public PayPalRequest<SellerCredentials> SellerCredentialsGet(string partnerId, string token)
        {
            var request = new PayPalRequest<SellerCredentials>(FormatPath("/v1/customer/partners/{0}/merchant-integrations/credentials", partnerId), HttpMethod.Get);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        #endregion

        #region Notification

        /// <summary>
        /// Creates a webhook.
        /// </summary>
        public PayPalRequest<Webhook, Webhook> WebhookCreate(Webhook? webhook)
            => new(webhook, "/v1/notifications/webhooks", HttpMethod.Post);

        /// <summary>
        /// Lists webhooks for an app.
        /// </summary>
        public PayPalRequest<Webhooks> WebhooksList()
            => new("/v1/notifications/webhooks", HttpMethod.Get);

        /// <summary>
        /// Verifies a webhook request by checking its signature against the API.
        /// </summary>
        public PayPalRequest<VerifyWebhookSignature<T>, object> WebhookVerifySignature<T>(VerifyWebhookSignature<T>? body) where T : class
            => new(body, "/v1/notifications/verify-webhook-signature?", HttpMethod.Post);

        #endregion

        #region Order

        /// <summary>
        /// Adds a tracking number to a PayPal order.
        /// </summary>
        public PayPalRequest<TrackingMessage, object> OrderAddTracking(TrackingMessage? body, string orderId)
            => new(body, FormatPath("/v2/checkout/orders/{0}/track", orderId), HttpMethod.Post);

        /// <summary>
        /// Creates a PayPal order.
        /// </summary>
        public PayPalRequest<OrderMessage, object> OrderCreate(OrderMessage? body)
            => new(body, "/v2/checkout/orders", HttpMethod.Post);

        /// <summary>
        /// Authorizes payment for an order. The response shows details of authorizations. 
        /// You can make this call only if you specified `intent=AUTHORIZE` in the create order call.
        /// </summary>
        public PayPalRequest<object> OrderAuthorize(string captureId)
            => new(FormatPath("/v2/checkout/orders/{0}/authorize?", captureId), HttpMethod.Post);

        /// <summary>
        /// Captures a payment for an order.
        /// </summary>
        public PayPalRequest<object> OrderCapture(string orderId)
            => new(FormatPath("/v2/checkout/orders/{0}/capture?", orderId), HttpMethod.Post);

        /// <summary>
        /// Shows details for an order by ID.
        /// </summary>
        public PayPalRequest<OrderMessage> OrderGet(string orderId)
            => new(FormatPath("/v2/checkout/orders/{0}?", orderId), HttpMethod.Get);

        /// <summary>
        /// Updates an order that has the `CREATED` or `APPROVED` status. You cannot update an order with `COMPLETED` status.
        /// </summary>
        public PayPalRequest<List<Patch<TPatch>>, object> OrderPatch<TPatch>(List<Patch<TPatch>>? body, string orderId)
            => new(body, FormatPath("/v2/checkout/orders/{0}?", orderId), HttpMethod.Patch);

        /// <summary>
        /// Updates a tracking number of a PayPal order.
        /// </summary>
        public PayPalRequest<List<Patch<string>>, object> OrderUpdateTracking(List<Patch<string>>? body, string orderId, string trackerId)
            => new(body, FormatPath("/v2/checkout/orders/{0}/trackers/{1}", orderId, trackerId), HttpMethod.Patch);

        #endregion

        #region Payment

        /// <summary>
        /// Captures an authorized order, by ID.
        /// </summary>
        public PayPalRequest<CaptureMessage, CaptureMessage> AuthorizationCapture(CaptureMessage? body, string captureId)
            => new(body, FormatPath("/v2/payments/authorizations/{0}/capture?", captureId), HttpMethod.Post);

        /// <summary>
        /// Voids or cancels an authorized payment, by ID. You cannot void an authorized payment that has been fully captured.
        /// </summary>
        public PayPalRequest AuthorizationVoid(string authorizationId)
            => new(FormatPath("/v2/payments/authorizations/{0}/void?", authorizationId), HttpMethod.Post);

        /// <summary>
        /// Refunds a captured payment, by ID. For a full refund, include an empty payload in the JSON request body. 
        /// For a partial refund, include an <code>amount</code> object in the JSON request body.
        /// </summary>
        public PayPalRequest<RefundMessage, RefundMessage> CaptureRefund(RefundMessage? body, string captureId)
            => new(body, FormatPath("/v2/payments/captures/{0}/refund?", captureId), HttpMethod.Post);

        #endregion

        #region Plan

        /// <summary>
        /// Activates a PayPal plan.
        /// </summary>
        public PayPalRequest PlanActivate(string planId)
            => new(FormatPath("/v1/billing/plans/{0}/activate", planId), HttpMethod.Post);

        /// <summary>
        /// Creates a PayPal plan.
        /// </summary>
        public PayPalRequest<Plan, object> PlanCreate(Plan? body)
            => new(body, "/v1/billing/plans", HttpMethod.Post);

        /// <summary>
        /// Deactivates a PayPal plan.
        /// </summary>
        public PayPalRequest PlanDeactivate(string planId)
            => new(FormatPath("/v1/billing/plans/{0}/deactivate", planId), HttpMethod.Post);

        /// <summary>
        /// Gets plan details.
        /// </summary>
        public PayPalRequest<Plan> PlanGet(string planId)
            => new(FormatPath("/v1/billing/plans/{0}", planId), HttpMethod.Get);

        #endregion

        #region Product

        /// <summary>
        /// Creates a product.
        /// </summary>
        public PayPalRequest<ProductMessage, object> ProductCreate(ProductMessage? body)
            => new(body, "/v1/catalogs/products", HttpMethod.Post);

        #endregion

        #region Subscription

        /// <summary>
        /// Activates a subscription.
        /// </summary>
        public PayPalRequest<SubscriptionStateChangeMessage, object> SubscriptionActivate(SubscriptionStateChangeMessage? body, string subscriptionId)
            => new(body, FormatPath("/v1/billing/subscriptions/{0}/activate", subscriptionId), HttpMethod.Post);

        /// <summary>
        /// Creates a subscription.
        /// </summary>
        public PayPalRequest<Subscription, object> SubscriptionCreate(Subscription? body)
            => new(body, "/v1/billing/subscriptions", HttpMethod.Post);

        /// <summary>
        /// Cancels a subscription.
        /// </summary>
        public PayPalRequest<SubscriptionStateChangeMessage, object> SubscriptionCancel(SubscriptionStateChangeMessage? body, string subscriptionId)
            => new(body, FormatPath("/v1/billing/subscriptions/{0}/cancel", subscriptionId), HttpMethod.Post);

        /// <summary>
        /// Suspends a subscription.
        /// </summary>
        public PayPalRequest<SubscriptionStateChangeMessage, object> SubscriptionSuspend(SubscriptionStateChangeMessage? body, string subscriptionId)
            => new(body, FormatPath("/v1/billing/subscriptions/{0}/suspend", subscriptionId), HttpMethod.Post);

        #endregion
    }
}
