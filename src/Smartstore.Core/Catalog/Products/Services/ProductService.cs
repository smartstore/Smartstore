using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductService : IProductService
    {
        private readonly SmartDbContext _db;

        public ProductService(
            SmartDbContext db)
        {
            _db = db;
        }

        public virtual async Task<IList<Product>> GetLowStockProductsAsync(bool tracked = false)
        {
            var query = _db.Products
                .ApplyTracking(tracked)
                .ApplyStandardFilter(true);

            // Track inventory for product.
            var query1 = 
                from p in query
                orderby p.MinStockQuantity
                where p.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock && p.MinStockQuantity >= p.StockQuantity
                select p;

            var products1 = await query1.ToListAsync();

            // Track inventory for product by product attributes.
            var query2 = 
                from p in query
                from pvac in p.ProductVariantAttributeCombinations
                where p.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes && pvac.StockQuantity <= 0
                select p;

            var products2 = await query2.ToListAsync();

            var result = new List<Product>();
            result.AddRange(products1);
            result.AddRange(products2);

            return result;
        }

        public virtual async Task<Multimap<int, ProductTag>> GetProductTagsByProductIdsAsync(int[] productIds, bool includeHidden = false)
        {
            Guard.NotNull(productIds, nameof(productIds));

            var map = new Multimap<int, ProductTag>();
            if (!productIds.Any())
            {
                return map;
            }

            var query = _db.Products
                .AsNoTracking()
                .Include(x => x.ProductTags)
                .ApplyStandardFilter(includeHidden)
                .Where(x => productIds.Contains(x.Id));

            if (!includeHidden)
            {
                // Only tags of products that are fully visible.
                query = query.Where(x => x.Visibility == ProductVisibility.Full);
            }

            var items = await query
                .Select(x => new
                {
                    ProductId = x.Id,
                    Tags = x.ProductTags.Where(y => includeHidden || y.Published)
                })
                .ToListAsync();

            foreach (var item in items)
            {
                map.AddRange(item.ProductId, item.Tags);
            }

            return map;
        }

        public virtual async Task<Multimap<int, Discount>> GetAppliedDiscountsByProductIdsAsync(int[] productIds, bool includeHidden = false)
        {
            Guard.NotNull(productIds, nameof(productIds));

            var map = new Multimap<int, Discount>();
            if (!productIds.Any())
            {
                return map;
            }

            // NoTracking does not seem to eager load here.
            var query = _db.Products
                .Include(x => x.AppliedDiscounts.Select(y => y.RuleSets))
                .ApplyStandardFilter(includeHidden)
                .Where(x => productIds.Contains(x.Id))
                .Select(x => new
                {
                    ProductId = x.Id,
                    Discounts = x.AppliedDiscounts
                });

            var items = await query.ToListAsync();

            foreach (var item in items)
            {
                map.AddRange(item.ProductId, item.Discounts);
            }

            return map;
        }
    }
}
