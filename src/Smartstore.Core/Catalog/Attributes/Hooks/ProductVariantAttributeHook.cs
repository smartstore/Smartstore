using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
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
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductVariantAttribute>()
                .Select(x => x.Id)
                .ToList();

            if (deletedAttributeIds.Any())
            {
                await _db.ProductBundleItemAttributeFilter
                    .Where(x => deletedAttributeIds.Contains(x.AttributeId))
                    .BatchDeleteAsync(cancelToken);
            }
        }
    }
}
