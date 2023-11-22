using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Pricing
{
    [Important]
    internal class TierPriceHook : AsyncDbSaveHook<TierPrice>
    {
        private readonly SmartDbContext _db;

        public TierPriceHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnInsertingAsync(TierPrice entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletingAsync(TierPrice entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Process products that have assigned tier prices.
            var addedTierPrices = entries
                .Where(x => x.State == EntityState.Added)
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToList();

            var addedTierPricesProductIds = addedTierPrices.ToDistinctArray(x => x.ProductId);
            if (addedTierPricesProductIds.Length > 0)
            {
                await _db.Products
                    .Where(x => addedTierPricesProductIds.Contains(x.Id))
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.HasTierPrices, p => true), cancelToken);
            }

            // Process products that have not assigned tier prices.
            var deletedTierPrices = entries
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToList();

            var deletedTierPricesProductIds = deletedTierPrices.ToDistinctArray(x => x.ProductId);
            if (deletedTierPricesProductIds.Length > 0)
            {
                var deletedTierPricesIds = deletedTierPrices
                    .Select(x => x.Id)
                    .ToArray();

                await _db.Products
                    .Where(x => deletedTierPricesProductIds.Contains(x.Id) &&
                        !x.TierPrices.Where(y => !deletedTierPricesIds.Contains(y.Id)).Any())
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.HasTierPrices, p => false), cancelToken);
            }
        }
    }
}
