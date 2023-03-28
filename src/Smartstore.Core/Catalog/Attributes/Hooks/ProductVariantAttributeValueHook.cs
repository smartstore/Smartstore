using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    [Important]
    internal class ProductVariantAttributeValueHook : AsyncDbSaveHook<ProductVariantAttributeValue>
    {
        private readonly SmartDbContext _db;

        public ProductVariantAttributeValueHook(SmartDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Sets all product variant attribute values to <see cref="ProductVariantAttributeValue.IsPreSelected"/> = false if the currently inserted entity is preselected.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(ProductVariantAttributeValue entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetPreselectedProductVariantAttributeValues(entity, cancelToken);
            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all product variant attribute values to <see cref="ProductVariantAttributeValue.IsPreSelected"/> = false if the currently updated entity is preselected.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(ProductVariantAttributeValue entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await ResetPreselectedProductVariantAttributeValues(entity, cancelToken);
            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(ProductVariantAttributeValue entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedValues = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductVariantAttributeValue>()
                .ToList();

            foreach (var deletedValue in deletedValues)
            {
                await _db.ProductBundleItemAttributeFilter
                    .Where(x => x.AttributeId == deletedValue.ProductVariantAttributeId && x.AttributeValueId == deletedValue.Id)
                    .ExecuteDeleteAsync(cancelToken);
            }
        }

        private async Task ResetPreselectedProductVariantAttributeValues(ProductVariantAttributeValue entity, CancellationToken cancelToken)
        {
            Guard.NotNull(entity);

            if (entity.IsPreSelected)
            {
                var productVariantAttributeValues = await _db.ProductVariantAttributeValues
                    .Where(x => x.IsPreSelected && x.ProductVariantAttributeId == entity.ProductVariantAttributeId && x.Id != entity.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.IsPreSelected, p => false), cancelToken);
            }
        }
    }
}
