using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductHook : AsyncDbSaveHook<Product>
    {
        private readonly SmartDbContext _db;

        public ProductHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override async Task<HookResult> OnDeletingAsync(Product entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Do not physically delete products.
            entry.State = Smartstore.Data.EntityState.Modified;

            entity.Deleted = true;
            entity.DeliveryTimeId = null;
            entity.QuantityUnitId = null;
            entity.CountryOfOriginId = null;

            if (entity.ProductType == ProductType.GroupedProduct)
            {
                var associatedProducts = await _db.Products
                    .Where(x => x.ParentGroupedProductId == entity.Id)
                    .ToListAsync(cancelToken);

                associatedProducts.Each(x => x.ParentGroupedProductId = 0);
            }

            await _db.SaveChangesAsync(cancelToken);

            return HookResult.Ok;
        }
    }
}
