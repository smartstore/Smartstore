namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Adds a tracking number to a PayPal order.
    /// </summary>
    public class OrderAddTrackingRequest(string orderId) : PayPalRequest<OrderAddTrackingRequest, object>(FormatPath("/v2/checkout/orders/{0}/track", orderId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Creates a PayPal order.
    /// </summary>
    public class OrderCreateRequest : PayPalRequest<OrderCreateRequest, OrderMessage>
    {
        public OrderCreateRequest()
            : base("/v2/checkout/orders", HttpMethod.Post)
        {
        }
    }

    /// <summary>
    /// Authorizes payment for an order. The response shows details of authorizations. 
    /// You can make this call only if you specified `intent=AUTHORIZE` in the create order call.
    /// </summary>
    public class OrdersAuthorizeRequest(string captureId) : PayPalRequest<OrdersAuthorizeRequest, object>(FormatPath("/v2/checkout/orders/{0}/authorize?", captureId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Captures a payment for an order.
    /// </summary>
    public class OrdersCaptureRequest(string orderId) : PayPalRequest<OrdersCaptureRequest, object>(FormatPath("/v2/checkout/orders/{0}/capture?", orderId), HttpMethod.Post)
    {
    }

    /// <summary>
    /// Shows details for an order by ID.
    /// </summary>
    public class OrdersGetRequest(string orderId) : PayPalRequest<OrderMessage>(FormatPath("/v2/checkout/orders/{0}?", orderId), HttpMethod.Get)
    {
    }

    /// <summary>
    /// Updates an order that has the `CREATED` or `APPROVED` status. You cannot update an order with `COMPLETED` status.
    /// </summary>
    public class OrdersPatchRequest<TPatch>(string orderId) : PayPalRequest<OrdersPatchRequest<TPatch>, List<Patch<TPatch>>>(FormatPath("/v2/checkout/orders/{0}?", orderId), HttpMethod.Patch)
    {
    }

    /// <summary>
    /// Updates a tracking number of a PayPal order.
    /// </summary>
    public class OrderUpdateTrackingRequest(string orderId, string trackerId) : PayPalRequest<OrderUpdateTrackingRequest, List<Patch<string>>>(FormatPath("/v2/checkout/orders/{0}/trackers/{1}", orderId, trackerId), HttpMethod.Patch)
    {
    }
}
