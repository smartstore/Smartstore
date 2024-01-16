using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    [Important]
    internal class ProductVariantAttributeHook : AsyncDbSaveHook<ProductVariantAttribute>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _deletedAttributeValueIds = [];

        public ProductVariantAttributeHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletingAsync(ProductVariantAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(ProductVariantAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedAttributeIds = GetDeletedAttributeIds(entries);
            if (deletedAttributeIds.Count > 0)
            {
                _deletedAttributeValueIds.AddRange(await _db.ProductVariantAttributeValues
                    .Where(x => deletedAttributeIds.Contains(x.ProductVariantAttributeId))
                    .Select(x => x.Id)
                    .ToListAsync(cancelToken));
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedAttributeIds = GetDeletedAttributeIds(entries);
            if (deletedAttributeIds.Count > 0)
            {
                await _db.ProductBundleItemAttributeFilter
                    .Where(x => deletedAttributeIds.Contains(x.AttributeId))
                    .ExecuteDeleteAsync(cancelToken);
            }

            // Delete localized properties of attribute values that were deleted by referential integrity.
            // Cannot be done by ProductVariantAttributeValueHook.
            if (_deletedAttributeValueIds.Count > 0)
            {
                foreach (var valueIdsChunk in _deletedAttributeValueIds.Chunk(100))
                {
                    await _db.LocalizedProperties
                        .Where(x => valueIdsChunk.Contains(x.EntityId) && x.LocaleKeyGroup == nameof(ProductVariantAttributeValue))
                        .ExecuteDeleteAsync(cancelToken);
                }

                _deletedAttributeValueIds.Clear();
            }
        }

        private static List<int> GetDeletedAttributeIds(IEnumerable<IHookedEntity> entries)
        {
            return entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductVariantAttribute>()
                .Select(x => x.Id)
                .ToList();
        }
    }
}
