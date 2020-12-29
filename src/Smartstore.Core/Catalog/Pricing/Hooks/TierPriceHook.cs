using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Pricing
{
    public class TierPriceHook : AsyncDbSaveHook<TierPrice>
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
                .Where(x => x.State == Smartstore.Data.EntityState.Added)
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToList();

            var addedTierPricesProductIds = addedTierPrices
                .Select(x => x.ProductId)
                .Distinct()
                .ToArray();

            if (addedTierPricesProductIds.Any())
            {
                await _db.Products
                    .Where(x => addedTierPricesProductIds.Contains(x.Id))
                    .BatchUpdateAsync(x => new Product { HasTierPrices = true });
            }

            // Process products that have not assigned tier prices.
            var deletedTierPrices = entries
                .Where(x => x.State == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToList();

            var deletedTierPricesProductIds = deletedTierPrices
                .Select(x => x.ProductId)
                .Distinct()
                .ToArray();

            if (deletedTierPricesProductIds.Any())
            {
                var deletedTierPricesIds = deletedTierPrices
                    .Select(x => x.Id)
                    .ToArray();

                await _db.Products
                    .Where(x =>
                        deletedTierPricesProductIds.Contains(x.Id) &&
                        !x.TierPrices.Where(y => !deletedTierPricesIds.Contains(y.Id)).Any())
                    .BatchUpdateAsync(x => new Product { HasTierPrices = false });
            }
        }
    }
}
