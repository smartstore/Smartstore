using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    [Important]
    internal class ProductVariantAttributeHook : AsyncDbSaveHook<ProductVariantAttribute>
    {
        private readonly SmartDbContext _db;

        public ProductVariantAttributeHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(ProductVariantAttribute entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedAttributeIds = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductVariantAttribute>()
                .Select(x => x.Id)
                .ToList();

            if (deletedAttributeIds.Count > 0)
            {
                await _db.ProductBundleItemAttributeFilter
                    .Where(x => deletedAttributeIds.Contains(x.AttributeId))
                    .ExecuteDeleteAsync(cancelToken);
            }
        }
    }
}
