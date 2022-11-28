using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Holds cargo data to reduce database roundtrips during price calculation or long running operations (like export etc.).
    /// </summary>
    public class ProductBatchContext
    {
        protected readonly List<int> _productIds = new();
        private readonly List<int> _productIdsTierPrices = new();
        private readonly List<int> _productIdsAppliedDiscounts = new();
        private readonly List<int> _bundledProductIds = new();
        private readonly List<int> _groupedProductIds = new();
        private readonly List<int> _mainMediaFileIds = new();

        protected ICommonServices _services;
        protected IProductService _productService;
        protected ICategoryService _categoryService;
        protected IManufacturerService _manufacturerService;
        protected readonly SmartDbContext _db;
        protected readonly bool _includeHidden;
        protected readonly bool _loadMainMediaOnly;

        private LazyMultimap<ProductVariantAttribute> _attributes;
        private LazyMultimap<ProductVariantAttributeCombination> _attributeCombinations;
        private LazyMultimap<TierPrice> _tierPrices;
        private LazyMultimap<ProductCategory> _productCategories;
        private LazyMultimap<ProductManufacturer> _productManufacturers;
        private LazyMultimap<Discount> _appliedDiscounts;
        private LazyMultimap<ProductBundleItem> _productBundleItems;
        private LazyMultimap<Product> _associatedProducts;
        private LazyMultimap<ProductMediaFile> _productMediaFiles;
        private LazyMultimap<ProductTag> _productTags;
        private LazyMultimap<ProductSpecificationAttribute> _specificationAttributes;
        private LazyMultimap<Download> _downloads;

        public ProductBatchContext(
            IEnumerable<Product> products,
            ICommonServices services,
            Store store,
            Customer customer,
            bool includeHidden,
            bool loadMainMediaOnly = false)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(store, nameof(store));
            Guard.NotNull(customer, nameof(customer));

            _services = services;
            _db = services.DbContext;
            Store = store;
            Customer = customer;
            _includeHidden = includeHidden;
            _loadMainMediaOnly = loadMainMediaOnly;

            if (products != null)
            {
                _productIds.AddRange(products.Select(x => x.Id));
                _productIdsTierPrices.AddRange(products.Where(x => x.HasTierPrices).Select(x => x.Id));
                _productIdsAppliedDiscounts.AddRange(products.Where(x => x.HasDiscountsApplied).Select(x => x.Id));
                _bundledProductIds.AddRange(products.Where(x => x.ProductType == ProductType.BundledProduct).Select(x => x.Id));
                _groupedProductIds.AddRange(products.Where(x => x.ProductType == ProductType.GroupedProduct).Select(x => x.Id));

                if (loadMainMediaOnly)
                {
                    _mainMediaFileIds.AddRange(products.Select(x => x.MainPictureId ?? 0).Where(x => x != 0).Distinct());
                }
            }
        }

        internal IProductService ProductService
        {
            get => _productService ??= _services.Resolve<IProductService>();
            // For testing purposes
            set => _productService = value;
        }

        internal ICategoryService CategoryService
        {
            get => _categoryService ??= _services.Resolve<ICategoryService>();
            // For testing purposes
            set => _categoryService = value;
        }

        internal IManufacturerService ManufacturerService
        {
            get => _manufacturerService ??= _services.Resolve<IManufacturerService>();
            // For testing purposes
            set => _manufacturerService = value;
        }

        public Store Store { get; }
        public Customer Customer { get; }

        public IReadOnlyList<int> ProductIds => _productIds;

        public LazyMultimap<ProductVariantAttribute> Attributes
        {
            get => _attributes ??= new LazyMultimap<ProductVariantAttribute>(keys => LoadAttributes(keys), _productIds);
        }

        public LazyMultimap<ProductVariantAttributeCombination> AttributeCombinations
        {
            get => _attributeCombinations ??= new LazyMultimap<ProductVariantAttributeCombination>(keys => LoadAttributeCombinations(keys), _productIds);
        }

        public LazyMultimap<TierPrice> TierPrices
        {
            get => _tierPrices ??= new LazyMultimap<TierPrice>(keys => LoadTierPrices(keys), _productIdsTierPrices);
        }

        public LazyMultimap<ProductCategory> ProductCategories
        {
            get => _productCategories ??= new LazyMultimap<ProductCategory>(keys => LoadProductCategories(keys), _productIds);
        }

        public LazyMultimap<ProductManufacturer> ProductManufacturers
        {
            get => _productManufacturers ??= new LazyMultimap<ProductManufacturer>(keys => LoadProductManufacturers(keys), _productIds);
        }

        public LazyMultimap<Discount> AppliedDiscounts
        {
            get => _appliedDiscounts ??= new LazyMultimap<Discount>(keys => LoadAppliedDiscounts(keys), _productIdsAppliedDiscounts);
        }

        public LazyMultimap<ProductBundleItem> ProductBundleItems
        {
            get => _productBundleItems ??= new LazyMultimap<ProductBundleItem>(keys => LoadProductBundleItems(keys), _bundledProductIds);
        }

        public LazyMultimap<Product> AssociatedProducts
        {
            get => _associatedProducts ??= new LazyMultimap<Product>(keys => LoadAssociatedProducts(keys), _groupedProductIds);
        }

        public LazyMultimap<ProductMediaFile> ProductMediaFiles
        {
            get => _productMediaFiles ??= new LazyMultimap<ProductMediaFile>(keys => LoadProductMediaFiles(keys), _productIds);
        }

        public LazyMultimap<ProductTag> ProductTags
        {
            get => _productTags ??= new LazyMultimap<ProductTag>(keys => LoadProductTags(keys), _productIds);
        }

        public LazyMultimap<ProductSpecificationAttribute> SpecificationAttributes
        {
            get => _specificationAttributes ??= new LazyMultimap<ProductSpecificationAttribute>(keys => LoadSpecificationAttributes(keys), _productIds);
        }

        public LazyMultimap<Download> Downloads
        {
            get => _downloads ??= new LazyMultimap<Download>(keys => LoadDownloads(keys), _productIds);
        }

        public virtual void Collect(IEnumerable<int> productIds)
        {
            Attributes.Collect(productIds);
            AttributeCombinations.Collect(productIds);
            TierPrices.Collect(productIds);
            ProductCategories.Collect(productIds);
            AppliedDiscounts.Collect(productIds);
            ProductBundleItems.Collect(productIds);
            AssociatedProducts.Collect(productIds);
        }

        public virtual void Clear()
        {
            _attributes?.Clear();
            _attributeCombinations?.Clear();
            _tierPrices?.Clear();
            _productCategories?.Clear();
            _productManufacturers?.Clear();
            _appliedDiscounts?.Clear();
            _productBundleItems?.Clear();
            _associatedProducts?.Clear();
            _bundledProductIds?.Clear();
            _groupedProductIds?.Clear();
            _productMediaFiles?.Clear();
            _productTags?.Clear();
            _specificationAttributes?.Clear();
            _downloads?.Clear();
        }

        #region Protected factories

        static readonly Func<SmartDbContext, int[], IAsyncEnumerable<ProductVariantAttribute>> CompiledAttributesQuery
            = EF.CompileAsyncQuery((SmartDbContext db, int[] ids) =>
                db.ProductVariantAttributes
                    .AsNoTracking()
                    .Include(x => x.ProductAttribute)
                    .Include(x => x.ProductVariantAttributeValues)
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .ThenBy(x => x.DisplayOrder)
                    .AsQueryable());
        protected virtual async Task<Multimap<int, ProductVariantAttribute>> LoadAttributes(int[] ids)
        {
            var attributes = await CompiledAttributesQuery(_db, ids).ToListAsync();
            return attributes.ToMultimap(x => x.ProductId, x => x);
        }


        static readonly Func<SmartDbContext, int[], IAsyncEnumerable<ProductVariantAttributeCombination>> CompiledAttributeCombinationsQuery
            = EF.CompileAsyncQuery((SmartDbContext db, int[] ids) =>
                db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .AsQueryable());
        protected virtual async Task<Multimap<int, ProductVariantAttributeCombination>> LoadAttributeCombinations(int[] ids)
        {
            var attributeCombinations = await CompiledAttributeCombinationsQuery(_db, ids).ToListAsync();
            return attributeCombinations.ToMultimap(x => x.ProductId, x => x);
        }


        static readonly Func<SmartDbContext, int[], int, IAsyncEnumerable<TierPrice>> CompiledTierPricesQuery
            = EF.CompileAsyncQuery((SmartDbContext db, int[] ids, int storeId) =>
                db.TierPrices
                    .AsNoTracking()
                    .Include(x => x.CustomerRole)
                    .Where(x => ids.Contains(x.ProductId) && (x.StoreId == 0 || x.StoreId == storeId))
                    .AsQueryable());
        protected virtual async Task<Multimap<int, TierPrice>> LoadTierPrices(int[] ids)
        {
            var tierPrices = await CompiledTierPricesQuery(_db, ids, Store.Id).ToListAsync();

            return tierPrices
                // Sorting locally is most likely faster.
                .OrderBy(x => x.ProductId)
                .ThenBy(x => x.Quantity)
                .FilterForCustomer(Customer)
                .ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductCategory>> LoadProductCategories(int[] ids)
        {
            var productCategories = await CategoryService.GetProductCategoriesByProductIdsAsync(ids, _includeHidden);
            return productCategories.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductManufacturer>> LoadProductManufacturers(int[] ids)
        {
            var productManufacturers = await ManufacturerService.GetProductManufacturersByProductIdsAsync(ids, _includeHidden);
            return productManufacturers.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, Discount>> LoadAppliedDiscounts(int[] ids)
        {
            // INFO: AppliedDiscounts.RuleSets are not eager loaded anymore to avoid MultipleCollectionIncludeWarning.
            var discounts = await _db.Products
                .AsNoTracking()
                .Include(x => x.AppliedDiscounts)
                .Where(x => ids.Contains(x.Id))
                .Select(x => new
                {
                    ProductId = x.Id,
                    Discounts = x.AppliedDiscounts
                })
                .ToListAsync();

            var map = new Multimap<int, Discount>();
            discounts.Each(x => map.AddRange(x.ProductId, x.Discounts));

            return map;
        }

        protected virtual async Task<Multimap<int, ProductBundleItem>> LoadProductBundleItems(int[] ids)
        {
            var bundleItemsQuery = _db.ProductBundleItem
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.BundleProduct)
                .Include(x => x.AttributeFilters)
                .ApplyBundledProductsFilter(ids, _includeHidden);

            var bundleItems = await bundleItemsQuery.ToListAsync();

            return bundleItems.ToMultimap(x => x.BundleProductId, x => x);
        }

        protected virtual async Task<Multimap<int, Product>> LoadAssociatedProducts(int[] ids)
        {
            var associatedProducts = await _db.Products
                .AsNoTracking()
                .ApplyAssociatedProductsFilter(ids, _includeHidden)
                .ToListAsync();

            return associatedProducts.ToMultimap(x => x.ParentGroupedProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductMediaFile>> LoadProductMediaFiles(int[] ids)
        {
            List<ProductMediaFile> files = null;

            var query = _db.ProductMediaFiles
                .AsNoTracking()
                .Include(x => x.MediaFile);

            if (_loadMainMediaOnly)
            {
                files = _mainMediaFileIds.Any()
                    ? await query.Where(x => _mainMediaFileIds.Contains(x.MediaFileId)).ToListAsync()
                    : new List<ProductMediaFile>();
            }
            else
            {
                files = await query
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .ThenBy(x => x.DisplayOrder)
                    .ToListAsync();
            }

            return files.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductTag>> LoadProductTags(int[] ids)
        {
            return await ProductService.GetProductTagsByProductIdsAsync(ids, _includeHidden);
        }

        protected virtual async Task<Multimap<int, ProductSpecificationAttribute>> LoadSpecificationAttributes(int[] ids)
        {
            var attributes = await _db.ProductSpecificationAttributes
                .AsNoTracking()
                .Include(x => x.SpecificationAttributeOption)
                .ThenInclude(x => x.SpecificationAttribute)
                .ApplyProductsFilter(ids)
                .ToListAsync();

            return attributes.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, Download>> LoadDownloads(int[] ids)
        {
            var downloads = await _db.Downloads
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyEntityFilter(nameof(Product), ids)
                .OrderBy(x => x.FileVersion)
                .ToListAsync();

            return downloads.ToMultimap(x => x.EntityId, x => x);
        }

        #endregion
    }
}