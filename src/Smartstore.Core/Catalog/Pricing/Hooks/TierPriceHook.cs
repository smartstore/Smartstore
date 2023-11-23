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

        protected override Task<HookResult> OnInsertedAsync(TierPrice entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(TierPrice entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Process added tier prices.
            var addedTierPricesProductIds = entries
                .Where(x => x.InitialState == EntityState.Added)
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToDistinctArray(x => x.ProductId);

            if (addedTierPricesProductIds.Length > 0)
            {
                await _db.Products
                    .Where(x => addedTierPricesProductIds.Contains(x.Id) && x.TierPrices.Any())
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.HasTierPrices, p => true), cancelToken);
            }

            // Process deleted tier prices.
            var deletedTierPricesProductIds = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToDistinctArray(x => x.ProductId);

            if (deletedTierPricesProductIds.Length > 0)
            {
                await _db.Products
                    .Where(x => deletedTierPricesProductIds.Contains(x.Id) && !x.TierPrices.Any())
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.HasTierPrices, p => false), cancelToken);
            }
        }
    }
}
