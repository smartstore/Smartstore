using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Stores;
using Smartstore.Data;

namespace Smartstore.Core.Catalog.Products
{
    public partial class StockSubscriptionService : IStockSubscriptionService
    {
        private readonly SmartDbContext _db;
        private readonly IMessageFactory _messageFactory;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly CatalogSettings _catalogSettings;

        public StockSubscriptionService(
            SmartDbContext db,
            IMessageFactory messageFactory,
            IWorkContext workContext,
            IStoreContext storeContext,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _messageFactory = messageFactory;
            _workContext = workContext;
            _storeContext = storeContext;
            _catalogSettings = catalogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual Task<bool> IsSubscribedAsync(Product product, Customer customer = null, int? storeId = null)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;
            storeId ??= _storeContext.CurrentStore.Id;

            return _db.BackInStockSubscriptions
                .ApplyStandardFilter(product.Id, customer.Id, storeId)
                .AnyAsync();
        }

        public virtual async Task<(bool Success, string Message)> SubscribeAsync(
            Product product,
            Customer customer = null,
            int? storeId = null,
            bool unsubscribe = false)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;
            storeId ??= _storeContext.CurrentStore.Id;

            var success = false;
            string message = null;

            if (customer.IsRegistered())
            {
                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                    product.BackorderMode == BackorderMode.NoBackorders &&
                    product.AllowBackInStockSubscriptions &&
                    product.StockQuantity <= 0)
                {
                    // Product is out of stock.
                    var subscription = await GetSubscriptionAsync(product, customer, storeId.Value);
                    if (subscription == null)
                    {
                        var subscriptionsCount = await _db.BackInStockSubscriptions
                            .Where(x => !x.Product.Deleted)
                            .ApplyStandardFilter(null, customer.Id, storeId)
                            .CountAsync();

                        if (subscriptionsCount < _catalogSettings.MaximumBackInStockSubscriptions)
                        {
                            // Subscribe.
                            _db.BackInStockSubscriptions.Add(new BackInStockSubscription
                            {
                                CustomerId = customer.Id,
                                ProductId = product.Id,
                                StoreId = storeId.Value,
                                CreatedOnUtc = DateTime.UtcNow
                            });

                            await _db.SaveChangesAsync();

                            success = true;
                            message = T("BackInStockSubscriptions.Subscribed");
                        }
                        else
                        {
                            message = T("BackInStockSubscriptions.MaxSubscriptions").Value.FormatInvariant(_catalogSettings.MaximumBackInStockSubscriptions);
                        }
                    }
                    else if (unsubscribe)
                    {
                        _db.BackInStockSubscriptions.Remove(subscription);
                        await _db.SaveChangesAsync();

                        success = true;
                        message = T("BackInStockSubscriptions.Unsubscribed");
                    }
                    else
                    {
                        message = T("BackInStockSubscriptions.AlreadySubscribed");
                    }
                }
                else
                {
                    message = T("BackInStockSubscriptions.NotAllowed");
                }
            }
            else
            {
                message = T("BackInStockSubscriptions.OnlyRegistered");
            }

            return (success, message);
        }

        public virtual async Task<(bool Success, string Message)> UnsubscribeAsync(Product product, Customer customer = null, int? storeId = null)
        {
            Guard.NotNull(product, nameof(product));

            var success = false;
            string message = null;

            var subscription = await GetSubscriptionAsync(product, customer ?? _workContext.CurrentCustomer, storeId ?? _storeContext.CurrentStore.Id);
            if (subscription != null)
            {
                _db.BackInStockSubscriptions.Remove(subscription);
                await _db.SaveChangesAsync();

                success = true;
                message = T("BackInStockSubscriptions.Unsubscribed");
            }
            else
            {
                message = T("Admin.Common.ResourceNotFound");
            }

            return (success, message);
        }

        public virtual async Task<int> SendNotificationsToSubscribersAsync(Product product)
        {
            Guard.NotNull(product, nameof(product));

            var numberOfMessages = 0;
            var subscriptionQuery = _db.BackInStockSubscriptions.ApplyStandardFilter(product.Id, null, null);
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

        protected virtual async Task<BackInStockSubscription> GetSubscriptionAsync(Product product, Customer customer, int storeId)
        {
            return await _db.BackInStockSubscriptions
                .ApplyStandardFilter(product.Id, customer.Id, storeId)
                .FirstOrDefaultAsync();
        }
    }
}
