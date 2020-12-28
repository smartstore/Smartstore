using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
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

        protected override Task<HookResult> OnInsertedAsync(TierPrice entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(TierPrice entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var tierPrices = entries
                .Select(x => x.Entity)
                .OfType<TierPrice>()
                .ToList();

            // Update HasTierPrices product property.
            if (tierPrices.Any())
            {
                var productIds = tierPrices
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToArray();

                foreach (var productIdsChunk in productIds.Slice(100))
                {
                    var tierPricesProductIds = await _db.TierPrices
                        .Where(x => productIdsChunk.Contains(x.ProductId))
                        .Select(x => x.ProductId)
                        .Distinct()
                        .ToListAsync();

                    var products = await _db.Products
                        .Where(x => productIdsChunk.Contains(x.Id))
                        .ToListAsync();

                    foreach (var product in products)
                    {
                        product.HasTierPrices = tierPricesProductIds.Contains(product.Id);
                    }

                    await _db.SaveChangesAsync();
                }
            }
        }
    }
}
