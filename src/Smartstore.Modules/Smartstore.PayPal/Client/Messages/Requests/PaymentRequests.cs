namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures an authorized order, by ID.
    /// </summary>
    public class AuthorizationsCaptureRequest(string captureId) : PayPalRequest<AuthorizationsCaptureRequest, CaptureMessage>(FormatPath("/v2/payments/authorizations/{0}/capture?", captureId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Voids or cancels an authorized payment, by ID. You cannot void an authorized payment that has been fully captured.
    /// </summary>
    public class AuthorizationsVoidRequest(string authorizationId) : PayPalRequest(FormatPath("/v2/payments/authorizations/{0}/void?", authorizationId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Refunds a captured payment, by ID. For a full refund, include an empty payload in the JSON request body. 
    /// For a partial refund, include an <code>amount</code> object in the JSON request body.
    /// </summary>
    public class CapturesRefundRequest(string captureId) : PayPalRequest<CapturesRefundRequest, RefundMessage>(FormatPath("/v2/payments/captures/{0}/refund?", captureId), HttpMethod.Post)
    {
    }
}
