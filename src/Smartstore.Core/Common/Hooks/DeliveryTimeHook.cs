using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    [Important]
    internal class DeliveryTimeHook : AsyncDbSaveHook<DeliveryTime>
    {
        private readonly SmartDbContext _db;
        private string _hookErrorMessage;

        public DeliveryTimeHook(SmartDbContext db)
        {
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultDeliveryTimes(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all delivery times to <see cref="DeliveryTime.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultDeliveryTimes(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of delivery time if it's referenced in products or attribute combinations 
        /// and removes associations to deleted products and attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(DeliveryTime entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Remove associations to deleted products.
            var productsQuery = _db.Products
                .IgnoreQueryFilters()
                .Where(x => x.Deleted && x.DeliveryTimeId == entity.Id);

            var productsPager = new FastPager<Product>(productsQuery, 500);
            while ((await productsPager.ReadNextPageAsync<Product>(cancelToken)).Out(out var products))
            {
                if (products.Any())
                {
                    products.Each(x => x.DeliveryTimeId = null);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            // Remove associations to attribute combinations of deleted products.
            var attributeCombinationQuery =
                from ac in _db.ProductVariantAttributeCombinations
                join p in _db.Products.AsNoTracking().IgnoreQueryFilters() on ac.ProductId equals p.Id
                where p.Deleted && ac.DeliveryTimeId == entity.Id
                select ac;

            var attributeCombinationPager = new FastPager<ProductVariantAttributeCombination>(attributeCombinationQuery, 1000);
            while ((await attributeCombinationPager.ReadNextPageAsync<ProductVariantAttributeCombination>(cancelToken)).Out(out var attributeCombinations))
            {
                if (attributeCombinations.Any())
                {
                    attributeCombinations.Each(x => x.DeliveryTimeId = null);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            // Ensure not to delete the default delivery time.
            if (entity.IsDefault == true)
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.DeliveryTimes.CannotDeleteDefaultDeliveryTime");
            }

            // Ensure that there are no associations to active products or attribute combinations.
            if (await _db.Products.AnyAsync(x => x.DeliveryTimeId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.DeliveryTimeId == entity.Id), cancelToken))
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.DeliveryTimes.CannotDeleteAssignedProducts");
            }

            return HookResult.Ok;
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new SmartException(message);
            }

            return Task.CompletedTask;
        }

        private async Task ResetDefaultDeliveryTimes(DeliveryTime entity, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.IsDefault == true)
            {
                var dts = await _db.DeliveryTimes
                    .Where(x => x.IsDefault == true && x.Id != entity.Id)
                    .ToListAsync(cancelToken);

                if (dts.Any())
                {
                    dts.Each(x => x.IsDefault = false);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }
    }
}
