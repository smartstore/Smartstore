using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Holds cargo data to reduce database roundtrips during price calculation.
    /// </summary>
    public class PriceCalculationContext
    {
        protected readonly List<int> _productIds = new();
        private readonly List<int> _productIdsTierPrices = new();
        private readonly List<int> _productIdsAppliedDiscounts = new();
        private readonly List<int> _bundledProductIds = new();
        private readonly List<int> _groupedProductIds = new();

        protected readonly SmartDbContext _db;
        protected readonly ICategoryService _categoryService;
        protected readonly IManufacturerService _manufacturerService;
        protected readonly Store _store;
        protected readonly Customer _customer;
        protected readonly bool _includeHidden;

        private LazyMultimap<ProductVariantAttribute> _attributes;
        private LazyMultimap<ProductVariantAttributeCombination> _attributeCombinations;
        private LazyMultimap<TierPrice> _tierPrices;
        private LazyMultimap<ProductCategory> _productCategories;
        private LazyMultimap<ProductManufacturer> _productManufacturers;
        private LazyMultimap<Discount> _appliedDiscounts;
        private LazyMultimap<ProductBundleItem> _productBundleItems;
        private LazyMultimap<Product> _associatedProducts;

        public PriceCalculationContext(
            IEnumerable<Product> products,
            ICommonServices services,
            Store store,
            Customer customer,
            bool includeHidden)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(store, nameof(store));
            Guard.NotNull(customer, nameof(customer));

            _db = services.DbContext;
            _categoryService = services.Resolve<ICategoryService>();
            _manufacturerService = services.Resolve<IManufacturerService>();
            _store = store;
            _customer = customer;
            _includeHidden = includeHidden;

            if (products != null)
            {
                _productIds.AddRange(products.Select(x => x.Id));
                _productIdsTierPrices.AddRange(products.Where(x => x.HasTierPrices).Select(x => x.Id));
                _productIdsAppliedDiscounts.AddRange(products.Where(x => x.HasDiscountsApplied).Select(x => x.Id));
                _bundledProductIds.AddRange(products.Where(x => x.ProductType == ProductType.BundledProduct).Select(x => x.Id));
                _groupedProductIds.AddRange(products.Where(x => x.ProductType == ProductType.GroupedProduct).Select(x => x.Id));
            }
        }

        public IReadOnlyList<int> ProductIds => _productIds;

        public LazyMultimap<ProductVariantAttribute> Attributes
        {
            get => _attributes ??= 
                new LazyMultimap<ProductVariantAttribute>(keys => LoadAttributes(keys), _productIds);
        }

        public LazyMultimap<ProductVariantAttributeCombination> AttributeCombinations
        {
            get => _attributeCombinations ??= 
                new LazyMultimap<ProductVariantAttributeCombination>(keys => LoadAttributeCombinations(keys), _productIds);
        }

        public LazyMultimap<TierPrice> TierPrices
        {
            get => _tierPrices ??= 
                new LazyMultimap<TierPrice>(keys => LoadTierPrices(keys), _productIdsTierPrices);
        }

        public LazyMultimap<ProductCategory> ProductCategories
        {
            get => _productCategories ??= 
                new LazyMultimap<ProductCategory>(keys => LoadProductCategories(keys), _productIds);
        }

        public LazyMultimap<ProductManufacturer> ProductManufacturers
        {
            get => _productManufacturers ??= 
                new LazyMultimap<ProductManufacturer>(keys => LoadProductManufacturers(keys), _productIds);
        }

        public LazyMultimap<Discount> AppliedDiscounts
        {
            get => _appliedDiscounts ??= 
                new LazyMultimap<Discount>(keys => LoadAppliedDiscounts(keys), _productIdsAppliedDiscounts);
        }     

        public LazyMultimap<ProductBundleItem> ProductBundleItems
        {
            get => _productBundleItems ??= 
                new LazyMultimap<ProductBundleItem>(keys => LoadProductBundleItems(keys), _bundledProductIds);
        }

        public LazyMultimap<Product> AssociatedProducts
        {
            get => _associatedProducts ??= 
                new LazyMultimap<Product>(keys => LoadAssociatedProducts(keys), _groupedProductIds);
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
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, ProductVariantAttribute>> LoadAttributes(int[] ids)
        {
            var attributes = await _db.ProductVariantAttributes
                .AsNoTracking()
                .Include(x => x.ProductAttribute)
                .Include(x => x.ProductVariantAttributeValues)
                .Where(x => ids.Contains(x.ProductId))
                .OrderBy(x => x.ProductId)
                .ThenBy(x => x.DisplayOrder)
                .ToListAsync();

            return attributes.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductVariantAttributeCombination>> LoadAttributeCombinations(int[] ids)
        {
            var attributeCombinations = await _db.ProductVariantAttributeCombinations
                .AsNoTracking()
                .Where(x => ids.Contains(x.ProductId))
                .OrderBy(x => x.ProductId)
                .ToListAsync();

            return attributeCombinations.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, TierPrice>> LoadTierPrices(int[] ids)
        {
            var tierPrices = await _db.TierPrices
                .AsNoTracking()
                .Include(x => x.CustomerRole)
                .Where(x => ids.Contains(x.ProductId) && (x.StoreId == 0 || x.StoreId == _store.Id))
                .ToListAsync();

            return tierPrices
                // Sorting locally is most likely faster.
                .OrderBy(x => x.ProductId)
                .ThenBy(x => x.Quantity)
                .FilterForCustomer(_customer)
                .ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductCategory>> LoadProductCategories(int[] ids)
        {
            var productCategories = await _categoryService.GetProductCategoriesByProductIdsAsync(ids, _includeHidden);
            return productCategories.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductManufacturer>> LoadProductManufacturers(int[] ids)
        {
            var productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdsAsync(ids, _includeHidden);
            return productManufacturers.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, Discount>> LoadAppliedDiscounts(int[] ids)
        {
            var discounts = await _db.Products
                .AsNoTracking()
                .Include(x => x.AppliedDiscounts)
                    .ThenInclude(x => x.RuleSets)
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
                .Include(x => x.BundleProduct);

            var query =
                from pbi in bundleItemsQuery
                join p in _db.Products.AsNoTracking() on pbi.ProductId equals p.Id
                where ids.Contains(pbi.BundleProductId) && (_includeHidden || (pbi.Published && p.Published))
                orderby pbi.DisplayOrder
                select pbi;

            var bundleItems = await query.ToListAsync();

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

        #endregion
    }
}