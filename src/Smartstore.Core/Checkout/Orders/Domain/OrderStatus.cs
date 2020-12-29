namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents an order status
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
}