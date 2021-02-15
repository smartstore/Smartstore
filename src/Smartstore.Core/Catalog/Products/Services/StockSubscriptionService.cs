using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Core.Messages;
using Smartstore.Data;

namespace Smartstore.Core.Catalog.Products
{
    public partial class StockSubscriptionService : IStockSubscriptionService
    {
        // TODO: (mg) (core) API predictability: add Subscribe/Unsubscribe methods to IStockSubscriptionService  

        private readonly SmartDbContext _db;
        private readonly IMessageFactory _messageFactory;

        public StockSubscriptionService(SmartDbContext db, IMessageFactory messageFactory)
        {
            _db = db;
            _messageFactory = messageFactory;
        }

        public virtual async Task<int> SendNotificationsToSubscribersAsync(Product product)
        {
            Guard.NotNull(product, nameof(product));

            var numberOfMessages = 0;
            var subscriptionQuery = _db.BackInStockSubscriptions.ApplyStandardFilter(0, product.Id, 0);
            var pager = new FastPager<BackInStockSubscription>(subscriptionQuery);

            while ((await pager.ReadNextPageAsync<BackInStockSubscription>()).Out(out var subscriptions))
            {
                foreach (var subscription in subscriptions)
                {
                    // Ensure that the customer is registered (simple and fast way).
                    if (subscription?.Customer?.Email?.IsEmail() ?? false)
                    {
                        await _messageFactory.SendBackInStockNotificationAsync(subscription);
                        ++numberOfMessages;
                    }
                }

                _db.BackInStockSubscriptions.RemoveRange(subscriptions);

                await _db.SaveChangesAsync();
            }

            return numberOfMessages;
        }
    }
}
