using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents cargo data to reduce database round trips during price calculation.
    /// </summary>
    public class PriceCalculationContext
    {
        protected readonly List<int> _productIds = new();
        private readonly List<int> _productIdsTierPrices = new();
        private readonly List<int> _productIdsAppliedDiscounts = new();
        private readonly List<int> _bundledProductIds = new();
        private readonly List<int> _groupedProductIds = new();

        private LazyMultimap<ProductVariantAttribute> _attributes;
        private LazyMultimap<ProductVariantAttributeCombination> _attributeCombinations;
        private LazyMultimap<TierPrice> _tierPrices;
        private LazyMultimap<ProductCategory> _productCategories;
        private LazyMultimap<ProductManufacturer> _productManufacturers;
        private LazyMultimap<Discount> _appliedDiscounts;
        private LazyMultimap<ProductBundleItem> _productBundleItems;
        private LazyMultimap<Product> _associatedProducts;

        public PriceCalculationContext(IEnumerable<Product> products)
        {
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

        public Func<int[], Task<Multimap<int, ProductVariantAttribute>>> 
            AttributesFactory { get; init; }

        public Func<int[], Task<Multimap<int, ProductVariantAttributeCombination>>> 
            AttributeCombinationsFactory { get; init; }

        public Func<int[], Task<Multimap<int, TierPrice>>> 
            TierPricesFactory { get; init; }

        public Func<int[], Task<Multimap<int, ProductCategory>>> 
            ProductCategoriesFactory { get; init; }

        public Func<int[], Task<Multimap<int, ProductManufacturer>>> 
            ProductManufacturersFactory { get; init; }

        public Func<int[], Task<Multimap<int, Discount>>> 
            AppliedDiscountsFactory { get; init; }

        public Func<int[], Task<Multimap<int, ProductBundleItem>>> 
            ProductBundleItemsFactory { get; init; }

        public Func<int[], Task<Multimap<int, Product>>> 
            AssociatedProductsFactory { get; init; }

        public LazyMultimap<ProductVariantAttribute> Attributes
        {
            get => _attributes ??= new LazyMultimap<ProductVariantAttribute>(keys => AttributesFactory(keys), _productIds);
        }

        public LazyMultimap<ProductVariantAttributeCombination> AttributeCombinations
        {
            get => _attributeCombinations ??= new LazyMultimap<ProductVariantAttributeCombination>(keys => AttributeCombinationsFactory(keys), _productIds);
        }

        public LazyMultimap<TierPrice> TierPrices
        {
            get => _tierPrices ??= new LazyMultimap<TierPrice>(keys => TierPricesFactory(keys), _productIdsTierPrices);
        }

        public LazyMultimap<ProductCategory> ProductCategories
        {
            get => _productCategories ??= new LazyMultimap<ProductCategory>(keys => ProductCategoriesFactory(keys), _productIds);
        }

        public LazyMultimap<ProductManufacturer> ProductManufacturers
        {
            get => _productManufacturers ??= new LazyMultimap<ProductManufacturer>(keys => ProductManufacturersFactory(keys), _productIds);
        }

        public LazyMultimap<Discount> AppliedDiscounts
        {
            get => _appliedDiscounts ??= new LazyMultimap<Discount>(keys => AppliedDiscountsFactory(keys), _productIdsAppliedDiscounts);
        }     

        public LazyMultimap<ProductBundleItem> ProductBundleItems
        {
            get => _productBundleItems ??= new LazyMultimap<ProductBundleItem>(keys => ProductBundleItemsFactory(keys), _bundledProductIds);
        }

        public LazyMultimap<Product> AssociatedProducts
        {
            get => _associatedProducts ??= new LazyMultimap<Product>(keys => AssociatedProductsFactory(keys), _groupedProductIds);
        }

        public void Collect(IEnumerable<int> productIds)
        {
            Attributes.Collect(productIds);
            AttributeCombinations.Collect(productIds);
            TierPrices.Collect(productIds);
            ProductCategories.Collect(productIds);
            AppliedDiscounts.Collect(productIds);
            ProductBundleItems.Collect(productIds);
            AssociatedProducts.Collect(productIds);
        }

        public void Clear()
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
    }
}