namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents requirements of a standard checkout.
    /// </summary>
    /// <remarks>
    /// If you have custom checkout requirements/steps then you must check directly via 
    /// <see cref="ICheckoutFactory"/> whether they are required.
    /// </remarks>
    [Flags]
    public enum CheckoutRequirements
    {
        BillingAddress = 1 << 0,
        Shipping = 1 << 1,
        Payment = 1 << 2,

        All = BillingAddress | Shipping | Payment
    }

    /// <summary>
    /// Represents an order status.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 10,

        /// <summary>
        /// Processing
        /// </summary>
        Processing = 20,

        /// <summary>
        /// Complete
        /// </summary>
        Complete = 30,

        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled = 40
    }

    /// <summary>
    /// Represents a return request status.
    /// </summary>
    public enum ReturnRequestStatus
    {
        /// <summary>
        /// Pending.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Received.
        /// </summary>
        Received = 10,

        /// <summary>
        /// Return authorized.
        /// </summary>
        ReturnAuthorized = 20,

        /// <summary>
        /// Item(s) repaired.
        /// </summary>
        ItemsRepaired = 30,

        /// <summary>
        /// Item(s) refunded.
        /// </summary>
        ItemsRefunded = 40,

        /// <summary>
        /// Request rejected.
        /// </summary>
        RequestRejected = 50,

        /// <summary>
        /// Cancelled.
        /// </summary>
        Cancelled = 60
    }
}