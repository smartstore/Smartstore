using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    [Important]
    internal class QuantityUnitHook : AsyncDbSaveHook<QuantityUnit>
    {
        private readonly SmartDbContext _db;
        private string _hookErrorMessage;

        public QuantityUnitHook(SmartDbContext db)
        {
            _db = db;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultQuantityUnits(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetDefaultQuantityUnits(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of quantity unit if it's referenced by products or attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Remove associations to deleted products.
            var productsQuery = _db.Products
                .IgnoreQueryFilters()
                .Where(x => x.Deleted && x.QuantityUnitId == entity.Id);

            var productsPager = new FastPager<Product>(productsQuery, 500);
            while ((await productsPager.ReadNextPageAsync<Product>(cancelToken)).Out(out var products))
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                if (products.Any())
                {
                    products.Each(x => x.QuantityUnitId = null);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            // Remove associations to attribute combinations of deleted products.
            var attributeCombinationQuery =
                from ac in _db.ProductVariantAttributeCombinations
                join p in _db.Products.AsNoTracking().IgnoreQueryFilters() on ac.ProductId equals p.Id
                where p.Deleted && ac.QuantityUnitId == entity.Id
                select ac;

            var attributeCombinationPager = new FastPager<ProductVariantAttributeCombination>(attributeCombinationQuery, 1000);
            while ((await attributeCombinationPager.ReadNextPageAsync<ProductVariantAttributeCombination>(cancelToken)).Out(out var attributeCombinations))
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                if (attributeCombinations.Any())
                {
                    attributeCombinations.Each(x => x.QuantityUnitId = null);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            if (entity.IsDefault)
            {
                // Cannot delete the default quantity unit.
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.QuantityUnit.CannotDeleteDefaultQuantityUnit", entity.Name.NaIfEmpty());
            }
            else if (await _db.Products.AnyAsync(x => x.QuantityUnitId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.QuantityUnitId == entity.Id), cancelToken))
            {
                // Cannot delete if there are associations to active products or attribute combinations.
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.QuantityUnit.CannotDeleteAssignedProducts", entity.Name.NaIfEmpty());
            }

            return HookResult.Ok;
        }

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

        private async Task ResetDefaultQuantityUnits(QuantityUnit entity, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.IsDefault)
            {
                var quantityUnits = await _db.QuantityUnits
                    .Where(x => x.IsDefault && x.Id != entity.Id)
                    .ToListAsync(cancelToken);

                if (quantityUnits.Any())
                {
                    quantityUnits.Each(x => x.IsDefault = false);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }
    }
}