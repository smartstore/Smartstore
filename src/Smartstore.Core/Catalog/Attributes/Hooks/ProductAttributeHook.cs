using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductAttributeHook : AsyncDbSaveHook<ProductAttribute>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _deletedAttributeOptionIds = new();

        public ProductAttributeHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override async Task<HookResult> OnDeletingAsync(ProductAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var optionIdsQuery =
                from a in _db.ProductAttributes.AsNoTracking()
                from os in a.ProductAttributeOptionsSets
                from ao in os.ProductAttributeOptions
                where a.Id == entry.Entity.Id
                select ao.Id;

            var optionIds = await optionIdsQuery.ToListAsync(cancelToken);

            _deletedAttributeOptionIds.AddRange(optionIds);

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(ProductAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Delete localized properties of attribute values that were deleted by referential integrity.
            if (_deletedAttributeOptionIds.Any())
            {
                await _db.LocalizedProperties
                    .Where(x => _deletedAttributeOptionIds.Contains(x.EntityId) && x.LocaleKeyGroup == nameof(ProductAttributeOption))
                    .ExecuteDeleteAsync(cancelToken);

                _deletedAttributeOptionIds.Clear();
            }
        }
    }
}
