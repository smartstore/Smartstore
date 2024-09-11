using Smartstore.Caching;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Stores
{
    [Important]
    internal class StoreHook : AsyncDbSaveHook<Store>
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly ICacheManager _cache;

        private string _hookErrorMessage;

        public StoreHook(
            SmartDbContext db,
            IStoreContext storeContext,
            Lazy<ICurrencyService> currencyService,
            ICacheManager cache)
        {
            _db = db;
            _storeContext = storeContext;
            _currencyService = currencyService;
            _cache = cache;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override Task<HookResult> OnInsertingAsync(Store entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            FixStoreEntity(entity);
            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnUpdatingAsync(Store entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            FixStoreEntity(entity);
            return Task.FromResult(HookResult.Ok);
        }

        protected override async Task<HookResult> OnDeletingAsync(Store entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (_storeContext.GetCachedStores().Stores.Count == 1)
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.Stores.CannotDeleteLastStore");
            }
            else if (await _db.WalletHistory.AnyAsync(x => x.StoreId == entity.Id, cancelToken))
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.Stores.CannotDeleteStoreWithWalletPostings");
            }

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(Store entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedStoreIds = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<Store>()
                .Select(x => x.Id)
                .Where(x => x != 0)
                .ToList();

            if (deletedStoreIds.Count == 0)
            {
                return;
            }

            // Cascade delete entities referenced by deleted stores which would be unused/unproductive after their deletion.
            // Do not delete sales entities like ReturnRequest.
            await _db.Settings
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);

            await _db.ThemeVariables
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);

            await _db.BackInStockSubscriptions
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);

            await _db.NewsletterSubscriptions
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);

            // Delete AdminAreaStoreScopeConfiguration attribute of deleted stores.
            // Avoids displaying override checkboxes for settings without store scope configuration.
            var deletedStoreIdsStr = deletedStoreIds.Select(x => x.ToStringInvariant()).ToArray();

            await _db.GenericAttributes
                .Where(x => x.Key == SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration && deletedStoreIdsStr.Contains(x.Value) && x.KeyGroup == nameof(Customer))
                .ExecuteDeleteAsync(cancelToken);

            // When we had two stores and now have only one store, we also should delete all "per store" settings.
            var allStoreIds = await _db.Stores.Select(x => x.Id).ToListAsync(cancelToken);
            if (allStoreIds.Count == 1)
            {
                await _db.Settings
                    .Where(x => x.StoreId == allStoreIds[0])
                    .ExecuteDeleteAsync(cancelToken);
            }

            await _db.ShoppingCartItems
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);

            await _db.TierPrices
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);

            var numDeleted = await _db.StoreMappings
                .Where(x => deletedStoreIds.Contains(x.StoreId))
                .ExecuteDeleteAsync(cancelToken);
            if (numDeleted > 0)
            {
                await _cache.RemoveByPatternAsync(StoreMappingService.STOREMAPPING_SEGMENT_PATTERN);
            }

            // What about generic attributes of customers? Can be a lot.
        }

        private void FixStoreEntity(Store store)
        {
            // Ensure we have "/" at the end.
            store.Url = store.Url.EnsureEndsWith('/');

            if (store.PrimaryExchangeRateCurrencyId == 0)
            {
                // INFO: we have to do this becaue there is a foreign key constraint on these fields.
                store.PrimaryExchangeRateCurrencyId = _currencyService.Value.PrimaryExchangeCurrency.Id;
            }
        }
    }
}
