using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    [Important]
    internal class DeliveryTimeHook : AsyncDbSaveHook<DeliveryTime>
    {
        private readonly SmartDbContext _db;

        public DeliveryTimeHook(SmartDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await TryResetDefaultDeliveryTimesAsync(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await TryResetDefaultDeliveryTimesAsync(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of delivery time if it's referenced in products or attribute combinations 
        /// and removes associations to deleted products and attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Remove associations to deleted products.
            var productsQuery = _db.Products.Where(x => x.Deleted && x.DeliveryTimeId == entity.Id);

            var productsPager = new FastPager<Product>(productsQuery, 500);
            while ((await productsPager.ReadNextPageAsync<Product>()).Out(out var products))
            {
                if (products.Any())
                {
                    products.Each(x => x.DeliveryTimeId = null);
                }
            }

            var attributeCombinationQuery =
                from ac in _db.ProductVariantAttributeCombinations
                join p in _db.Products.AsNoTracking() on ac.ProductId equals p.Id
                where p.Deleted && ac.DeliveryTimeId == entity.Id
                select ac;

            var attributeCombinationPager = new FastPager<ProductVariantAttributeCombination>(attributeCombinationQuery, 1000);
            while ((await attributeCombinationPager.ReadNextPageAsync<ProductVariantAttributeCombination>()).Out(out var attributeCombinations))
            {
                if (attributeCombinations.Any())
                {
                    attributeCombinations.Each(x => x.DeliveryTimeId = null);
                }
            }

            // Warn and throw if there are associations to active products or attribute combinations.
            var query =
                from x in _db.Products
                where x.DeliveryTimeId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.DeliveryTimeId == entity.Id)
                select x.Id;

            if (await query.AnyAsync(cancellationToken: cancelToken))
            {
                // Prohibit saving of associated entities.
                entry.State = Smartstore.Data.EntityState.Detached;
                throw new SmartException("The delivery time cannot be deleted. It has associated products or product variants.");
            }

            return HookResult.Ok;
        }

        public virtual async Task TryResetDefaultDeliveryTimesAsync(DeliveryTime entity, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.IsDefault == true)
            {
                var dts = await _db.DeliveryTimes
                    .Where(x => x.IsDefault == true && x.Id != entity.Id)
                    .ToListAsync(cancellationToken: cancelToken);

                foreach (var dt in dts)
                {
                    dt.IsDefault = false;
                }
            }
        }
    }
}
