using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
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

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var softDeletedProducts = entries
                .Where(x => x.IsSoftDeleted == true)
                .Select(x => x.Entity)
                .OfType<Product>()
                .ToList();

            foreach (var product in softDeletedProducts)
            {
                product.Deleted = true;
                product.DeliveryTimeId = null;
                product.QuantityUnitId = null;
                product.CountryOfOriginId = null;
            }

            await _db.SaveChangesAsync();

            // Unassign grouped products
            var groupedProductIds = softDeletedProducts
                .Where(x => x.ProductType == ProductType.GroupedProduct)
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            var allAssociatedProducts = await _db.Products
                .Where(x => groupedProductIds.Contains(x.ParentGroupedProductId))
                .BatchUpdateAsync(x => new Product { ParentGroupedProductId = 0 });
        }
    }
}
