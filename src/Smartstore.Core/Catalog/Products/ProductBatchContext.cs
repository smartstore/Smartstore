using Autofac;
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
        protected readonly List<int> _productIds = [];
        private readonly List<int> _productIdsTierPrices = [];
        private readonly List<int> _productIdsAppliedDiscounts = [];
        private readonly List<int> _bundledProductIds = [];
        private readonly List<int> _groupedProductIds = [];
        private readonly List<int> _mainMediaFileIds = [];

        protected readonly IComponentContext _componentContext;
        protected readonly SmartDbContext _db;
        protected readonly bool _includeHidden;
        protected readonly bool _loadMainMediaOnly;

        protected IProductService _productService;
        protected ICategoryService _categoryService;
        protected IManufacturerService _manufacturerService;

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
        private LazyMultimap<ProductSpecificationAttribute> _essentialSpecAttributes;
        private LazyMultimap<Download> _downloads;
        private LazyMultimap<RelatedProduct> _relatedProducts;
        private LazyMultimap<CrossSellProduct> _crossSellProducts;

        public ProductBatchContext(
            IEnumerable<Product> products,
            SmartDbContext db,
            IComponentContext componentContext,
            Store store,
            Customer customer,
            bool includeHidden,
            bool loadMainMediaOnly = false)
        {
            _componentContext = Guard.NotNull(componentContext);
            Store = Guard.NotNull(store);
            Customer = Guard.NotNull(customer);
            _db = Guard.NotNull(db);

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
            get => _productService ??= _componentContext.Resolve<IProductService>();
            // For testing purposes
            set => _productService = value;
        }

        internal ICategoryService CategoryService
        {
            get => _categoryService ??= _componentContext.Resolve<ICategoryService>();
            // For testing purposes
            set => _categoryService = value;
        }

        internal IManufacturerService ManufacturerService
        {
            get => _manufacturerService ??= _componentContext.Resolve<IManufacturerService>();
            // For testing purposes
            set => _manufacturerService = value;
        }

        public Store Store { get; }
        public Customer Customer { get; }

        public IReadOnlyList<int> ProductIds => _productIds;

        public LazyMultimap<ProductVariantAttribute> Attributes
        {
            get => _attributes ??= new LazyMultimap<ProductVariantAttribute>(LoadAttributes, _productIds);
        }

        public LazyMultimap<ProductVariantAttributeCombination> AttributeCombinations
        {
            get => _attributeCombinations ??= new LazyMultimap<ProductVariantAttributeCombination>(LoadAttributeCombinations, _productIds);
        }

        public LazyMultimap<TierPrice> TierPrices
        {
            get => _tierPrices ??= new LazyMultimap<TierPrice>(LoadTierPrices, _productIdsTierPrices);
        }

        public LazyMultimap<ProductCategory> ProductCategories
        {
            get => _productCategories ??= new LazyMultimap<ProductCategory>(LoadProductCategories, _productIds);
        }

        public LazyMultimap<ProductManufacturer> ProductManufacturers
        {
            get => _productManufacturers ??= new LazyMultimap<ProductManufacturer>(LoadProductManufacturers, _productIds);
        }

        public LazyMultimap<Discount> AppliedDiscounts
        {
            get => _appliedDiscounts ??= new LazyMultimap<Discount>(LoadAppliedDiscounts, _productIdsAppliedDiscounts);
        }

        public LazyMultimap<ProductBundleItem> ProductBundleItems
        {
            get => _productBundleItems ??= new LazyMultimap<ProductBundleItem>(LoadProductBundleItems, _bundledProductIds);
        }

        public LazyMultimap<Product> AssociatedProducts
        {
            get => _associatedProducts ??= new LazyMultimap<Product>(LoadAssociatedProducts, _groupedProductIds);
        }

        public LazyMultimap<ProductMediaFile> ProductMediaFiles
        {
            get => _productMediaFiles ??= new LazyMultimap<ProductMediaFile>(LoadProductMediaFiles, _productIds);
        }

        public LazyMultimap<ProductTag> ProductTags
        {
            get => _productTags ??= new LazyMultimap<ProductTag>(LoadProductTags, _productIds);
        }

        public LazyMultimap<ProductSpecificationAttribute> SpecificationAttributes
        {
            get => _specificationAttributes ??= new LazyMultimap<ProductSpecificationAttribute>(LoadSpecificationAttributes, _productIds);
        }

        public LazyMultimap<ProductSpecificationAttribute> EssentialAttributes
        {
            get => _essentialSpecAttributes ??= new LazyMultimap<ProductSpecificationAttribute>(LoadEssentialAttributes, _productIds);
        }

        public LazyMultimap<Download> Downloads
        {
            get => _downloads ??= new LazyMultimap<Download>(LoadDownloads, _productIds);
        }

        public LazyMultimap<RelatedProduct> RelatedProducts
        {
            get => _relatedProducts ??= new LazyMultimap<RelatedProduct>(LoadRelatedProducts, _productIds);
        }

        public LazyMultimap<CrossSellProduct> CrossSellProducts
        {
            get => _crossSellProducts ??= new LazyMultimap<CrossSellProduct>(LoadCrossSellProducts, _productIds);
        }

        /// <summary>
        /// Adds more product identifiers. Enables the subsequent loading of products.
        /// </summary>
        public virtual void Collect(IEnumerable<int> productIds)
        {
            Attributes.Collect(productIds);
            AttributeCombinations.Collect(productIds);
            TierPrices.Collect(productIds);
            ProductCategories.Collect(productIds);
            ProductManufacturers.Collect(productIds);
            AppliedDiscounts.Collect(productIds);
            ProductBundleItems.Collect(productIds);
            AssociatedProducts.Collect(productIds);
            ProductMediaFiles.Collect(productIds);
            SpecificationAttributes.Collect(productIds);
            EssentialAttributes.Collect(productIds);
            Downloads.Collect(productIds);
            RelatedProducts.Collect(productIds);
            CrossSellProducts.Collect(productIds);
        }

        /// <summary>
        /// Clears all loaded data. All internal objects will be reset.
        /// </summary>
        public virtual void Clear()
        {
            _attributes?.Clear();
            _attributeCombinations?.Clear();
            _tierPrices?.Clear();
            _productIdsTierPrices?.Clear();
            _productCategories?.Clear();
            _productManufacturers?.Clear();
            _appliedDiscounts?.Clear();
            _productIdsAppliedDiscounts?.Clear();
            _productBundleItems?.Clear();
            _bundledProductIds?.Clear();
            _associatedProducts?.Clear();
            _groupedProductIds?.Clear();
            _productMediaFiles?.Clear();
            _productTags?.Clear();
            _specificationAttributes?.Clear();
            _essentialSpecAttributes?.Clear();
            _productMediaFiles?.Clear();
            _downloads?.Clear();
            _relatedProducts?.Clear();
            _crossSellProducts?.Clear();
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, ProductVariantAttribute>> LoadAttributes(int[] ids)
        {
            var query = _db.ProductVariantAttributes
                    .AsNoTracking()
                    .Include(x => x.ProductAttribute)
                    .Include(x => x.ProductVariantAttributeValues)
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .ThenBy(x => x.DisplayOrder)
                    .AsQueryable();
            var attributes = await query.ToListAsync();
            return attributes.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductVariantAttributeCombination>> LoadAttributeCombinations(int[] ids)
        {
            var query = _db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .AsQueryable();
            var attributeCombinations = await query.ToListAsync();
            return attributeCombinations.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, TierPrice>> LoadTierPrices(int[] ids)
        {
            var query = _db.TierPrices
                    .AsNoTracking()
                    .Include(x => x.CustomerRole)
                    .Where(x => ids.Contains(x.ProductId) && (x.StoreId == 0 || x.StoreId == Store.Id))
                    .AsQueryable();
            var tierPrices = await query.ToListAsync();

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
                .SelectSummary()
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
                files = _mainMediaFileIds.Count > 0
                    ? await query.Where(x => _mainMediaFileIds.Contains(x.MediaFileId)).ToListAsync()
                    : [];
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

        private static IQueryable<ProductSpecificationAttribute> BuildSpecAttributesQuery(SmartDbContext db, int[] ids, bool? essentialAttributes)
        {
            return db.ProductSpecificationAttributes
                .AsNoTracking()
                .Include(x => x.SpecificationAttributeOption)
                .ThenInclude(x => x.SpecificationAttribute)
                .Where(x => ids.Contains(x.ProductId) && (essentialAttributes == null || x.SpecificationAttributeOption.SpecificationAttribute.Essential == essentialAttributes.Value))
                .OrderBy(x => x.ProductId)
                .OrderBy(x => x.DisplayOrder);
        }
        protected virtual async Task<Multimap<int, ProductSpecificationAttribute>> LoadSpecificationAttributes(int[] ids)
        {
            var attributes = await BuildSpecAttributesQuery(_db, ids, null).ToListAsync();
            return attributes.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductSpecificationAttribute>> LoadEssentialAttributes(int[] ids)
        {
            if (SpecificationAttributes.FullyLoaded)
            {
                return SpecificationAttributes
                    .SelectMany(x => x.Value)
                    .Where(x => x.SpecificationAttributeOption.SpecificationAttribute.Essential)
                    .ToMultimap(x => x.ProductId, x => x);
            }

            var attributes = await BuildSpecAttributesQuery(_db, ids, true).ToListAsync();
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

        protected virtual async Task<Multimap<int, RelatedProduct>> LoadRelatedProducts(int[] ids)
        {
            var query =
                from rp in _db.RelatedProducts.AsNoTracking()
                join p in _db.Products.AsNoTracking() on rp.ProductId2 equals p.Id
                where ids.Contains(rp.ProductId1) && (_includeHidden || p.Published)
                orderby rp.DisplayOrder
                select rp;

            var relatedProducts = await query.ToListAsync();

            return relatedProducts.ToMultimap(x => x.ProductId1, x => x);
        }

        protected virtual async Task<Multimap<int, CrossSellProduct>> LoadCrossSellProducts(int[] ids)
        {
            var query =
                from csp in _db.CrossSellProducts
                join p in _db.Products.AsNoTracking() on csp.ProductId2 equals p.Id
                where ids.Contains(csp.ProductId1) && (_includeHidden || p.Published)
                orderby csp.Id
                select csp;

            var crossSellProducts = await query.ToListAsync();

            return crossSellProducts.ToMultimap(x => x.ProductId1, x => x);
        }

        #endregion
    }
}