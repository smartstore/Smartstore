using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    internal class ProductAttributeOptionsSetHook : AsyncDbSaveHook<ProductAttributeOptionsSet>
    {
        private readonly SmartDbContext _db;
        private readonly HashSet<int> _deletedAttributeOptionIds = new();

        public ProductAttributeOptionsSetHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletingAsync(ProductAttributeOptionsSet entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            _deletedAttributeOptionIds.AddRange(entity.ProductAttributeOptions.Select(x => x.Id));

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletedAsync(ProductAttributeOptionsSet entity, IHookedEntity entry, CancellationToken cancelToken)
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
