using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Back in stock subscription service.
    /// </summary>
    public partial interface IStockSubscriptionService
    {
        /// <summary>
        /// Gets a value indicating whether the customer is subscribed to stock notifications.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="customer">Customer entity. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="storeId">Store identifier. If <c>null</c>, identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>A value indicating whether the customer is subscribed to stock notifications.</returns>
        Task<bool> IsSubscribedAsync(Product product, Customer customer = null, int? storeId = null);

        /// <summary>
        /// Subscribes to stock notifications.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="customer">Customer entity. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="storeId">Store identifier. If <c>null</c>, identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <param name="unsubscribe">A value indicating whether to unsubscribe if already subscribed.</param>
        /// <returns>A value indicating whether the operation succeeded and a related message.</returns>
        Task<(bool Success, string Message)> SubscribeAsync(Product product, Customer customer = null, int? storeId = null, bool unsubscribe = false);

        /// <summary>
        /// Unsubscribes to stock notifications.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="customer">Customer entity. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="storeId">Store identifier. If <c>null</c>, identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <returns>A value indicating whether the operation succeeded and a related message.</returns>
        Task<(bool Success, string Message)> UnsubscribeAsync(Product product, Customer customer = null, int? storeId = null);

        /// <summary>
        /// Send notification to subscribers.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <returns>Number of sent emails.</returns>
        Task<int> SendNotificationsToSubscribersAsync(Product product);
    }
}
