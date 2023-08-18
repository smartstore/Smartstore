using System.Collections.Immutable;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductService : IProductService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICommonServices _services;
        private readonly Lazy<IProductTagService> _productTagService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IMessageFactory _messageFactory;
        private readonly LocalizationSettings _localizationSettings;

        public ProductService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICommonServices services,
            Lazy<IProductTagService> productTagService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IMessageFactory messageFactory,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _services = services;
            _productTagService = productTagService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _messageFactory = messageFactory;
            _localizationSettings = localizationSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task<(Product Product, ProductVariantAttributeCombination VariantCombination)> GetProductByCodeAsync(
            string code,
            bool includeHidden = false,
            bool tracked = false)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return (null, null);
            }

            var pq = _db.Products
                .ApplyTracking(tracked)
                .ApplyStandardFilter(includeHidden);

            if (!includeHidden)
            {
                pq = pq.Where(x => x.Visibility <= ProductVisibility.SearchResults);
            }

            var product = await pq
                .ApplyProductCodeFilter(code)
                .FirstOrDefaultAsync();

            if (product != null)
            {
                return (product, null);
            }

            var pvcq = _db.ProductVariantAttributeCombinations
                .Include(x => x.Product)
                .ApplyTracking(tracked)
                .ApplyStandardFilter(includeHidden)
                .ApplyProductCodeFilter(code);

            if (!includeHidden)
            {
                pvcq = pvcq.Where(x => x.Product.Visibility <= ProductVisibility.SearchResults);
            }

            var variantCombination = await pvcq.FirstOrDefaultAsync();

            return (variantCombination?.Product, variantCombination);
        }

        public virtual async Task<Multimap<int, ProductTag>> GetProductTagsByProductIdsAsync(int[] productIds, bool includeHidden = false)
        {
            Guard.NotNull(productIds);

            var map = new Multimap<int, ProductTag>();
            if (!productIds.Any())
            {
                return map;
            }

            var query = _db.Products
                .AsNoTracking()
                .Include(x => x.ProductTags)
                .Where(x => productIds.Contains(x.Id))
                .ApplyStandardFilter(includeHidden);

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

        public virtual async Task<IList<Product>> GetCrossSellProductsByProductIdsAsync(int[] productIds, int numberOfProducts, bool includeHidden = false)
        {
            Guard.NotNull(productIds);

            var result = new List<Product>();

            if (numberOfProducts == 0 || !productIds.Any())
            {
                return result;
            }

            var query =
                from csp in _db.CrossSellProducts.AsNoTracking()
                join p in _db.Products on csp.ProductId2 equals p.Id
                where productIds.Contains(csp.ProductId1) && (includeHidden || p.Published)
                orderby csp.Id
                select csp;

            var csItems = await query.ToListAsync();
            var productIds1 = new HashSet<int>(csItems
                .Select(x => x.ProductId2)
                .Except(productIds));

            if (productIds1.Any())
            {
                var productIds2 = productIds1.Take(numberOfProducts).ToArray();

                var products = await _db.Products
                    .AsNoTracking()
                    .Where(x => productIds2.Contains(x.Id))
                    .SelectSummary()
                    .ToListAsync();

                result.AddRange(products.OrderBySequence(productIds2));
            }

            return result;
        }

        public virtual void ApplyProductReviewTotals(Product product)
        {
            Guard.NotNull(product);

            // TODO: (core) Make a faster ApplyProductReviewTotals later without the necessity to eager load reviews.

            var approvedRatingSum = 0;
            var notApprovedRatingSum = 0;
            var approvedTotalReviews = 0;
            var notApprovedTotalReviews = 0;
            var reviews = product.ProductReviews;

            foreach (var pr in reviews)
            {
                if (pr.IsApproved)
                {
                    approvedRatingSum += pr.Rating;
                    approvedTotalReviews++;
                }
                else
                {
                    notApprovedRatingSum += pr.Rating;
                    notApprovedTotalReviews++;
                }
            }

            product.ApprovedRatingSum = approvedRatingSum;
            product.NotApprovedRatingSum = notApprovedRatingSum;
            product.ApprovedTotalReviews = approvedTotalReviews;
            product.NotApprovedTotalReviews = notApprovedTotalReviews;
        }

        public virtual async Task<AdjustInventoryResult> AdjustInventoryAsync(OrderItem orderItem, bool decrease, int quantity)
        {
            Guard.NotNull(orderItem);

            if (orderItem.Product.ProductType == ProductType.BundledProduct && orderItem.Product.BundlePerItemShoppingCart)
            {
                if (orderItem.BundleData.HasValue())
                {
                    var bundleData = orderItem.GetBundleData();
                    if (bundleData.Any())
                    {
                        var productIds = bundleData
                            .Select(x => x.ProductId)
                            .Distinct()
                            .ToArray();

                        var products = await _db.Products
                            .Where(x => productIds.Contains(x.Id))
                            .ToListAsync();

                        var productsDic = products.ToDictionary(x => x.Id);

                        foreach (var item in bundleData)
                        {
                            if (productsDic.TryGetValue(item.ProductId, out var product))
                            {
                                await AdjustInventoryAsync(product, item.AttributeSelection, decrease, quantity * item.Quantity);
                            }
                        }
                    }
                }

                return new AdjustInventoryResult();
            }
            else
            {
                return await AdjustInventoryAsync(orderItem.Product, orderItem.AttributeSelection, decrease, quantity);
            }
        }

        public virtual async Task<AdjustInventoryResult> AdjustInventoryAsync(Product product, ProductVariantAttributeSelection selection, bool decrease, int quantity)
        {
            Guard.NotNull(product);

            var result = new AdjustInventoryResult();

            switch (product.ManageInventoryMethod)
            {
                case ManageInventoryMethod.ManageStock:
                {
                    result.StockQuantityOld = product.StockQuantity;

                    result.StockQuantityNew = decrease
                        ? product.StockQuantity - quantity
                        : product.StockQuantity + quantity;

                    var newPublished = product.Published;
                    var newDisableBuyButton = product.DisableBuyButton;
                    var newDisableWishlistButton = product.DisableWishlistButton;

                    // Check if the minimum quantity is reached.
                    switch (product.LowStockActivity)
                    {
                        case LowStockActivity.DisableBuyButton:
                            newDisableBuyButton = product.MinStockQuantity >= result.StockQuantityNew;
                            newDisableWishlistButton = product.MinStockQuantity >= result.StockQuantityNew;
                            break;
                        case LowStockActivity.Unpublish:
                            newPublished = product.MinStockQuantity <= result.StockQuantityNew;
                            break;
                    }

                    product.StockQuantity = result.StockQuantityNew;
                    product.DisableBuyButton = newDisableBuyButton;
                    product.DisableWishlistButton = newDisableWishlistButton;
                    product.Published = newPublished;

                    // SaveChanges is not necessary because SendQuantityBelowStoreOwnerNotificationAsync
                    // does not reload anything that has been changed in the meantime.

                    if (decrease && product.NotifyAdminForQuantityBelow > result.StockQuantityNew)
                    {
                        await _messageFactory.SendQuantityBelowStoreOwnerNotificationAsync(product, _localizationSettings.DefaultAdminLanguageId);
                    }
                }
                break;
                case ManageInventoryMethod.ManageStockByAttributes:
                {
                    var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection);
                    if (combination != null)
                    {
                        _db.TryChangeState(combination, EfState.Unchanged);

                        result.StockQuantityOld = combination.StockQuantity;

                        result.StockQuantityNew = decrease
                            ? combination.StockQuantity - quantity
                            : combination.StockQuantity + quantity;

                        combination.StockQuantity = result.StockQuantityNew;
                    }
                }
                break;
                case ManageInventoryMethod.DontManageStock:
                default:
                    // Do nothing.
                    break;
            }

            if (selection != null)
            {
                var attributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);

                var productLinkageValues = attributeValues
                    .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage)
                    .ToList();

                foreach (var chunk in productLinkageValues.Chunk(100))
                {
                    var linkedProductIds = chunk.Select(x => x.LinkedProductId).Distinct().ToArray();
                    var linkedProducts = await _db.Products.GetManyAsync(linkedProductIds, true);
                    var linkedProductsDic = linkedProducts.ToDictionarySafe(x => x.Id);

                    foreach (var value in chunk)
                    {
                        if (linkedProductsDic.TryGetValue(value.LinkedProductId, out var linkedProduct))
                        {
                            await AdjustInventoryAsync(linkedProduct, null, decrease, quantity * value.Quantity);
                        }
                    }
                }
            }

            return result;
        }

        public virtual async Task<int> EnsureMutuallyRelatedProductsAsync(int productId1)
        {
            var productQuery = _db.Products.ApplyStandardFilter(true);

            var relatedProductIdsQuery =
                from rp in _db.RelatedProducts
                join p in productQuery on rp.ProductId2 equals p.Id
                where rp.ProductId1 == productId1
                orderby rp.DisplayOrder
                select rp.ProductId2;

            var productIds = await relatedProductIdsQuery.ToListAsync();

            if (productIds.Count > 0 && !productIds.Any(x => x == productId1))
            {
                productIds.Add(productId1);
            }
            if (!productIds.Any())
            {
                return 0;
            }

            var query =
                from rp in _db.RelatedProducts
                join p in _db.Products on rp.ProductId2 equals p.Id
                where productIds.Contains(rp.ProductId2)
                select new { rp.ProductId1, rp.ProductId2 };

            var allAssociatedIds = await query.ToListAsync();
            var associatedIdsMap = allAssociatedIds.ToMultimap(x => x.ProductId2, x => x.ProductId1);
            var displayOrders = new Dictionary<int, int>();

            foreach (var id1 in productIds)
            {
                var associatedIds = associatedIdsMap.ContainsKey(id1)
                    ? associatedIdsMap[id1]
                    : new List<int>();

                foreach (var id2 in productIds)
                {
                    if (id1 != id2 && !associatedIds.Any(x => x == id2))
                    {
                        if (!displayOrders.ContainsKey(id2))
                        {
                            displayOrders[id2] = await _db.RelatedProducts
                                .Where(x => x.ProductId1 == id2)
                                .OrderByDescending(x => x.DisplayOrder)
                                .Select(x => x.DisplayOrder)
                                .FirstOrDefaultAsync() + 1;
                        }
                        else
                        {
                            displayOrders[id2] = displayOrders[id2] + 1;
                        }

                        _db.RelatedProducts.Add(new RelatedProduct
                        {
                            ProductId1 = id2,
                            ProductId2 = id1,
                            DisplayOrder = displayOrders[id2]
                        });
                    }
                }
            }

            return await _db.SaveChangesAsync();
        }

        public virtual async Task<int> EnsureMutuallyCrossSellProductsAsync(int productId1)
        {
            var productQuery = _db.Products.ApplyStandardFilter(true);

            var crossSellProductIdsQuery =
                from csp in _db.CrossSellProducts
                join p in productQuery on csp.ProductId2 equals p.Id
                where csp.ProductId1 == productId1
                orderby csp.Id
                select csp.ProductId2;

            var productIds = await crossSellProductIdsQuery.ToListAsync();

            if (productIds.Count > 0 && !productIds.Any(x => x == productId1))
            {
                productIds.Add(productId1);
            }
            if (!productIds.Any())
            {
                return 0;
            }

            var query =
                from csp in _db.CrossSellProducts
                join p in _db.Products on csp.ProductId2 equals p.Id
                where productIds.Contains(csp.ProductId2)
                select new { csp.ProductId1, csp.ProductId2 };

            var allAssociatedIds = await query.ToListAsync();
            var associatedIdsMap = allAssociatedIds.ToMultimap(x => x.ProductId2, x => x.ProductId1);

            foreach (var id1 in productIds)
            {
                var associatedIds = associatedIdsMap.ContainsKey(id1)
                    ? associatedIdsMap[id1]
                    : new List<int>();

                foreach (var id2 in productIds)
                {
                    if (id1 != id2 && !associatedIds.Any(x => x == id2))
                    {
                        _db.CrossSellProducts.Add(new CrossSellProduct
                        {
                            ProductId1 = id2,
                            ProductId2 = id1
                        });
                    }
                }
            }

            return await _db.SaveChangesAsync();
        }

        public virtual ProductBatchContext CreateProductBatchContext(
            IEnumerable<Product> products = null,
            Store store = null,
            Customer customer = null,
            bool includeHidden = true,
            bool loadMainMediaOnly = false)
        {
            return new ProductBatchContext(
                products,
                _services,
                store ?? _storeContext.CurrentStore,
                customer ?? _workContext.CurrentCustomer,
                includeHidden,
                loadMainMediaOnly);
        }

        #region Recycle bin

        public virtual async Task<int> RestoreProductsAsync(int[] productIds)
        {
            if (productIds.IsNullOrEmpty())
            {
                return 0;
            }

            var success = 0;
            var parentCategories = await _db.Categories
                .IgnoreQueryFilters()
                .Select(x => new { x.Id, x.ParentId })
                .ToDictionaryAsync(x => x.Id, x => x.ParentId ?? 0);

            foreach (var productIdsChunk in productIds.Chunk(100))
            {
                var products = await _db.Products
                    .IgnoreQueryFilters()
                    .Where(x => productIdsChunk.Contains(x.Id) && x.Deleted)
                    .Select(x => new RestoreProduct
                    {
                        Id = x.Id,
                        ProductTypeId = x.ProductTypeId
                    })
                    .ToListAsync();

                foreach (var product in products)
                {
                    try
                    {
                        await RestoreProductInternal(product, parentCategories);
                        success++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }

            if (success > 0)
            {
                await _productTagService.Value.ClearCacheAsync();
            }

            return success;
        }

        private async Task RestoreProductInternal(RestoreProduct product, Dictionary<int, int> parentCategories)
        {
            // Get IDs of products to restore.
            var productIds = new HashSet<int>(product.Id);

            if (product.ProductTypeId == (int)ProductType.BundledProduct)
            {
                productIds.AddRange(await _db.ProductBundleItem
                    .IgnoreQueryFilters()
                    .Where(x => x.BundleProductId == product.Id && x.Product.Deleted)
                    .Select(x => x.ProductId)
                    .ToArrayAsync());
            }

            var requiredProductIds = await _db.Products
                .IgnoreQueryFilters()
                .Where(x => productIds.Contains(x.Id) && x.RequireOtherProducts && x.Deleted)
                .Select(x => x.RequiredProductIds)
                .ToArrayAsync();

            foreach (var idsStr in requiredProductIds)
            {
                productIds.AddRange(idsStr.SplitSafe(',', StringSplitOptions.TrimEntries)
                    .Select(x => int.TryParse(x, out var id) ? id : int.MaxValue)
                    .Where(x => x < int.MaxValue));
            }

            // First restore products, then all other entities.
            await _db.Products
                .IgnoreQueryFilters()
                .Where(x => productIds.Contains(x.Id) && x.Deleted)
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.Deleted, m => false));

            // Restore manufacturers.
            var deletedManufacturerIds = await _db.ProductManufacturers
                .IgnoreQueryFilters()
                .Where(x => productIds.Contains(x.ProductId) && x.Manufacturer.Deleted)
                .Select(x => x.ManufacturerId)
                .Distinct()
                .ToArrayAsync();
            if (deletedManufacturerIds.Length > 0)
            {
                await _db.Manufacturers
                    .IgnoreQueryFilters()
                    .Where(x => deletedManufacturerIds.Contains(x.Id))
                    .ExecuteUpdateAsync(x => x.SetProperty(m => m.Deleted, m => false));
            }

            // Restore categories.
            var categoryIds = new HashSet<int>();
            var assignedCategoryIds = await _db.ProductCategories
                .IgnoreQueryFilters()
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => x.CategoryId)
                .Distinct()
                .ToArrayAsync();
            assignedCategoryIds.Each(GetCategoryIds);

            if (categoryIds.Count > 0)
            {
                await _db.Categories
                    .IgnoreQueryFilters()
                    .Where(x => categoryIds.Contains(x.Id) && x.Deleted)
                    .ExecuteUpdateAsync(x => x.SetProperty(c => c.Deleted, c => false));
            }

            void GetCategoryIds(int categoryId)
            {
                if (categoryId != 0)
                {
                    categoryIds.Add(categoryId);
                    if (parentCategories.TryGetValue(categoryId, out var parentId))
                    {
                        GetCategoryIds(parentId);
                    }
                }
            }
        }

        public virtual async Task<int> DeleteProductsPermanentAsync(int[] productIds)
        {
            if (productIds.IsNullOrEmpty())
            {
                return 0;
            }

            // INFO: relationship has a cascade delete rule (dangerous).
            var excludeProductIds = await _db.OrderItems
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => x.ProductId)
                .Distinct()
                .ToArrayAsync();

            if (excludeProductIds.Length > 0)
            {
                productIds = productIds.Except(excludeProductIds).ToArray();
            }

            var success = 0;

            foreach (var productIdsChunk in productIds.Chunk(50))
            {
                try
                {
                    // INFO: ISoftDeletable would prevent the product from being permanently deleted.
                    // Better avoid change tracking and hooks, at the cost of some duplicate code.
                    await DeleteProductsPermanentInternal(productIdsChunk);
                    success += productIdsChunk.Length;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return success;
        }

        private async Task DeleteProductsPermanentInternal(int[] productIds)
        {
            // INFO: not necessary here to check whether an assignment of certain type,
            // (e.g. if the deleted product is grouped or bundled product). Just remove any assignment.

            const string productEntityName = nameof(Product);

            // ----- Product assignments.

            await _db.Products
                .Where(x => productIds.Contains(x.ParentGroupedProductId))
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.ParentGroupedProductId, p => 0));

            // ----- Missing cascade delete rules.

            // Cart item of bundle items.
            var bundleItemIds = await _db.ProductBundleItem
                .Where(x => productIds.Contains(x.BundleProductId))
                .Select(x => x.Id)
                .Distinct()
                .ToArrayAsync();

            if (bundleItemIds.Length > 0)
            {
                await _db.ShoppingCartItems
                    .Where(x => x.BundleItemId != null && bundleItemIds.Contains(x.BundleItemId.Value))
                    .ExecuteDeleteAsync();
            }

            // Product review helpfulness.
            var reviewIds = await _db.ProductReviews
                .Where(x => productIds.Contains(x.Id))
                .Select(x => x.Id)
                .Distinct()
                .ToArrayAsync();

            if (reviewIds.Length > 0)
            {
                // TODO: (mg) test if CustomerContent is deleted by referential integrity here.
                await _db.ProductReviewHelpfulness
                    .Where(x => reviewIds.Contains(x.ProductReviewId))
                    .ExecuteDeleteAsync();
            }

            await _db.CrossSellProducts
                .Where(x => productIds.Contains(x.ProductId1) || productIds.Contains(x.ProductId2))
                .ExecuteDeleteAsync();

            // ----- Satellite tables.

            await _db.StoreMappings
                .Where(x => productIds.Contains(x.EntityId) && x.EntityName == productEntityName)
                .ExecuteDeleteAsync();

            await _db.AclRecords
                .Where(x => productIds.Contains(x.EntityId) && x.EntityName == productEntityName)
                .ExecuteDeleteAsync();

            await _db.UrlRecords
                .Where(x => productIds.Contains(x.EntityId) && x.EntityName == productEntityName)
                .ExecuteDeleteAsync();

            await _db.LocalizedProperties
                .Where(x => productIds.Contains(x.EntityId) && x.LocaleKeyGroup == productEntityName)
                .ExecuteDeleteAsync();

            await _db.SyncMappings
                .Where(x => productIds.Contains(x.EntityId) && x.EntityName == productEntityName)
                .ExecuteDeleteAsync();

            // TODO: plugin tables -> use hook.
        }

        class RestoreProduct
        {
            public int Id { get; set; }
            public int ProductTypeId { get; set; }
        }

        #endregion
    }
}
