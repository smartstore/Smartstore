using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    [Important]
    internal class ProductVariantAttributeHook : AsyncDbSaveHook<ProductVariantAttribute>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _deletedAttributeValueIds = new();

        public ProductVariantAttributeHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletingAsync(ProductVariantAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _deletedAttributeValueIds.AddRange(entity.ProductVariantAttributeValues.Select(x => x.Id));

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletedAsync(ProductVariantAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedAttributeIds = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductVariantAttribute>()
                .Select(x => x.Id)
                .ToList();

            if (deletedAttributeIds.Any())
            {
                await _db.ProductBundleItemAttributeFilter
                    .Where(x => deletedAttributeIds.Contains(x.AttributeId))
                    .ExecuteDeleteAsync(cancelToken);
            }

            // Delete localized properties of attribute values that were deleted by referential integrity.
            // Cannot be done by ProductVariantAttributeValueHook.
            if (_deletedAttributeValueIds.Any())
            {
                await _db.LocalizedProperties
                    .Where(x => _deletedAttributeValueIds.Contains(x.EntityId) && x.LocaleKeyGroup == nameof(ProductVariantAttributeValue))
                    .ExecuteDeleteAsync(cancelToken);

                _deletedAttributeValueIds.Clear();
            }
        }
    }
}
