using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Back in stock subscription service.
    /// </summary>
    public partial interface IStockSubscriptionService
    {
        /// <summary>
        /// Send notification to subscribers.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <returns>Number of sent emails.</returns>
        Task<int> SendNotificationsToSubscribersAsync(Product product);
    }
}
