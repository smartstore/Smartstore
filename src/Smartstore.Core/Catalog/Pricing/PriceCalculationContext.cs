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
        protected readonly List<int> _productIds;
        private readonly List<int> _productIdsTierPrices;
        private readonly List<int> _productIdsAppliedDiscounts;
        private readonly List<int> _bundledProductIds;
        private readonly List<int> _groupedProductIds;

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
            if (products == null)
            {
                _productIds = new List<int>();
                _productIdsTierPrices = new List<int>();
                _productIdsAppliedDiscounts = new List<int>();
                _bundledProductIds = new List<int>();
                _groupedProductIds = new List<int>();
            }
            else
            {
                _productIds = new List<int>(products.Select(x => x.Id));
                _productIdsTierPrices = new List<int>(products.Where(x => x.HasTierPrices).Select(x => x.Id));
                _productIdsAppliedDiscounts = new List<int>(products.Where(x => x.HasDiscountsApplied).Select(x => x.Id));
                _bundledProductIds = new List<int>(products.Where(x => x.ProductType == ProductType.BundledProduct).Select(x => x.Id));
                _groupedProductIds = new List<int>(products.Where(x => x.ProductType == ProductType.GroupedProduct).Select(x => x.Id));
            }
        }

        public IReadOnlyList<int> ProductIds => _productIds;

        public Func<int[], Task<Multimap<int, ProductVariantAttribute>>> AttributesFactory { get; init; }

        public LazyMultimap<ProductVariantAttribute> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new LazyMultimap<ProductVariantAttribute>(keys => AttributesFactory(keys), _productIds);
                }

                return _attributes;
            }
        }

        public Func<int[], Task<Multimap<int, ProductVariantAttributeCombination>>> AttributeCombinationsFactory { get; init; }

        public LazyMultimap<ProductVariantAttributeCombination> AttributeCombinations
        {
            get
            {
                if (_attributeCombinations == null)
                {
                    _attributeCombinations = new LazyMultimap<ProductVariantAttributeCombination>(keys => AttributeCombinationsFactory(keys), _productIds);
                }

                return _attributeCombinations;
            }
        }

        public Func<int[], Task<Multimap<int, TierPrice>>> TierPricesFactory { get; init; }

        public LazyMultimap<TierPrice> TierPrices
        {
            get
            {
                if (_tierPrices == null)
                {
                    _tierPrices = new LazyMultimap<TierPrice>(keys => TierPricesFactory(keys), _productIdsTierPrices);
                }

                return _tierPrices;
            }
        }

        public Func<int[], Task<Multimap<int, ProductCategory>>> ProductCategoriesFactory { get; init; }

        public LazyMultimap<ProductCategory> ProductCategories
        {
            get
            {
                if (_productCategories == null)
                {
                    _productCategories = new LazyMultimap<ProductCategory>(keys => ProductCategoriesFactory(keys), _productIds);
                }

                return _productCategories;
            }
        }

        public Func<int[], Task<Multimap<int, ProductManufacturer>>> ProductManufacturersFactory { get; init; }

        public LazyMultimap<ProductManufacturer> ProductManufacturers
        {
            get
            {
                if (_productManufacturers == null)
                {
                    _productManufacturers = new LazyMultimap<ProductManufacturer>(keys => ProductManufacturersFactory(keys), _productIds);
                }

                return _productManufacturers;
            }
        }

        public Func<int[], Task<Multimap<int, Discount>>> AppliedDiscountsFactory { get; init; }

        public LazyMultimap<Discount> AppliedDiscounts
        {
            get
            {
                if (_appliedDiscounts == null)
                {
                    _appliedDiscounts = new LazyMultimap<Discount>(keys => AppliedDiscountsFactory(keys), _productIdsAppliedDiscounts);
                }

                return _appliedDiscounts;
            }
        }

        public Func<int[], Task<Multimap<int, ProductBundleItem>>> ProductBundleItemsFactory { get; init; }

        public LazyMultimap<ProductBundleItem> ProductBundleItems
        {
            get
            {
                if (_productBundleItems == null)
                {
                    _productBundleItems = new LazyMultimap<ProductBundleItem>(keys => ProductBundleItemsFactory(keys), _bundledProductIds);
                }

                return _productBundleItems;
            }
        }

        public Func<int[], Task<Multimap<int, Product>>> AssociatedProductsFactory { get; init; }

        public LazyMultimap<Product> AssociatedProducts
        {
            get
            {
                if (_associatedProducts == null)
                {
                    _associatedProducts = new LazyMultimap<Product>(keys => AssociatedProductsFactory(keys), _groupedProductIds);
                }

                return _associatedProducts;
            }
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