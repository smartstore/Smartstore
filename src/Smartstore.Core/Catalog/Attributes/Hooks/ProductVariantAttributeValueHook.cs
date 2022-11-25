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

        protected override Task<HookResult> OnDeletedAsync(ProductVariantAttributeValue entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedValues = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
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
    }
}
