using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Core.Common.Hooks
{
    public class DeliveryTimeHook : AsyncDbSaveHook<DeliveryTime>
    {
        private readonly SmartDbContext _db;

        public DeliveryTimeHook(SmartDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.IsDefault == true)
            {
                var temp = new List<DeliveryTime> { entity };
                var query = await _db.DeliveryTimes
                    .Where(x => x.IsDefault == true && x.Id != entity.Id)
                    .ToListAsync();

                foreach (var dt in query)
                {
                    dt.IsDefault = false;
                }
            }

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of delivery time if it's referenced in products or attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // TODO: (MH) (core) Implement when attributes are available
            // Remove associations to deleted products.
            //using (var scope = new DbContextScope(_productRepository.Context, autoDetectChanges: false, validateOnSave: false, hooksEnabled: false, autoCommit: false))
            //{
            //    var productsQuery = _productRepository.Table.Where(x => x.Deleted && x.DeliveryTimeId == deliveryTime.Id);
            //    var productsPager = new FastPager<Product>(productsQuery, 500);

            //    while (productsPager.ReadNextPage(out var products))
            //    {
            //        if (products.Any())
            //        {
            //            products.Each(x => x.DeliveryTimeId = null);
            //            scope.Commit();
            //        }
            //    }

            //    var attributeCombinationQuery =
            //        from ac in _attributeCombinationRepository.Table
            //        join p in _productRepository.Table on ac.ProductId equals p.Id
            //        where p.Deleted && ac.DeliveryTimeId == deliveryTime.Id
            //        select ac;

            //    var attributeCombinationPager = new FastPager<ProductVariantAttributeCombination>(attributeCombinationQuery, 1000);

            //    while (attributeCombinationPager.ReadNextPage(out var attributeCombinations))
            //    {
            //        if (attributeCombinations.Any())
            //        {
            //            attributeCombinations.Each(x => x.DeliveryTimeId = null);
            //            scope.Commit();
            //        }
            //    }
            //}

            // IsAssociated
            var query =
                from x in _db.Products
                // TODO: (MH) (core) Implement ProductVariantAttributeCombinations
                //where x.DeliveryTimeId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.DeliveryTimeId == entity.Id)
                where x.DeliveryTimeId == entity.Id
                select x.Id;

            if (await query.AnyAsync(cancellationToken: cancelToken))
            {
                throw new SmartException("The delivery time cannot be deleted. It has associated product variants.");
            }

            return HookResult.Ok;
        }
    }
}
