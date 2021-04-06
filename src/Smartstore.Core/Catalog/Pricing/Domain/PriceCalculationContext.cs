using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Contains input data that price calculation pipelines require access to.
    /// </summary>
    public class PriceCalculationContext
    {
        private Product _product;
        private ICollection<TierPrice> _tierPrices;
        private List<ProductVariantAttributeValue> _preSelectedAttributeValues;

        /// <summary>
        /// Creates a new context instance for given <paramref name="product"/> and <paramref name="options"/>.
        /// </summary>
        /// <param name="product">The product to calculate price for.</param>
        /// <param name="options">The calculation options.</param>
        public PriceCalculationContext(Product product, PriceCalculationOptions options)
            : this(product, 1, options)
        {
        }

        /// <summary>
        /// Creates a new context instance for given <paramref name="product"/>, <paramref name="quantity"/> and <paramref name="options"/>.
        /// </summary>
        /// <param name="product">The product to calculate price for.</param>
        /// <param name="quantity">The product quantity.</param>
        /// <param name="options">The calculation options.</param>
        public PriceCalculationContext(Product product, int quantity, PriceCalculationOptions options)
        {
            Guard.NotNull(options, nameof(options));

            Product = product;
            Quantity = quantity;

            // Always work with a shallow copy of options
            Options = options.Clone();
        }

        protected PriceCalculationContext(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            Product = context.Product;
            Quantity = context.Quantity;
            Options = context.Options;
            Metadata = context.Metadata;
            AssociatedProducts = context.AssociatedProducts;
            BundleItems = context.BundleItems;
            BundleItem = context.BundleItem;
            SelectedAttributes = context.SelectedAttributes;
            AdditionalCharge = context.AdditionalCharge;
            // [...]
        }

        /// <summary>
        /// The input product to calculate price for.
        /// </summary>
        public Product Product
        {
            get => _product;
            set => _product = value ?? throw new ArgumentNullException(nameof(Product));
        }

        /// <summary>
        /// An explicit list of calculator instances that define the pipeline. A non-null array
        /// skips auto-resolution of calculators by <see cref="IPriceCalculatorFactory.GetCalculators(PriceCalculationContext)"/>.
        /// </summary>
        public IPriceCalculator[] Calculators { get; init; }

        /// <summary>
        /// The product quantity. May have impact on final price, e.g. because of tier prices etc.
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// The calculation options/settings.
        /// </summary>
        public PriceCalculationOptions Options { get; init; }

        /// <summary>
        /// Custom input metadata that can be evaluated by custom calculators.
        /// </summary>
        public Dictionary<string, object> Metadata { get; } = new();

        /// <summary>
        /// Contains all associated child products of a grouped product (<see cref="ProductType.GroupedProduct"/>).
        /// If null, associated products will be auto-resolved during pipeline execution (but only if product type is <see cref="ProductType.GroupedProduct"/>).
        /// For performance reasons, it is advisable that in batch calculation scenarios ALL associated products of ALL root products are loaded
        /// in one roundtrip.
        /// </summary>
        public ICollection<Product> AssociatedProducts { get; set; }

        /// <summary>
        /// Contains all bundle parts of a bundle product (<see cref="ProductType.BundledProduct"/>).
        /// If null, bundle parts will be auto-resolved during pipeline execution 
        /// (but only if product type is <see cref="ProductType.BundledProduct"/> and <see cref="Product.BundlePerItemPricing"/> is <c>true</c>).
        /// For performance reasons, it is advisable that in batch calculation scenarios ALL bundle parts of ALL root products are loaded
        /// in one roundtrip.
        /// </summary>
        public ICollection<ProductBundleItemData> BundleItems { get; set; }

        /// <summary>
        /// A single bundle part. Used by bundle price calculator in nested calculation pipeline.
        /// </summary>
        public ProductBundleItemData BundleItem { get; set; }

        /// <summary>
        /// Gets or sets the selected product attributes to be included in the price calculation.
        /// For example required to take into account price adjustments of attributes selected by the customer.
        /// It is recommended to use the <see cref="PriceCalculationContext"/> extension methods to apply these attributes, 
        /// e.g. <see cref="PriceCalculationContextExtensions.AddSelectedAttributes(PriceCalculationContext, IEnumerable{OrganizedShoppingCartItem})"/>
        /// to apply selected attributes of all products indcluded in a shopping cart.
        /// </summary>
        public List<PriceCalculationAttributes> SelectedAttributes { get; set; } = new();

        /// <summary>
        /// The additional charges applied to the <see cref="CalculatorContext.FinalPrice"/> during calculation, such as price adjustments of product attributes.
        /// </summary>
        public decimal AdditionalCharge { get; set; }

        /// <summary>
        /// Gets tier prices for <see cref="Product"/>. Tier prices with duplicate quantities are removed.
        /// </summary>
        public async Task<ICollection<TierPrice>> GetTierPricesAsync()
        {
            if (_tierPrices == null)
            {
                _tierPrices = (await Options.BatchContext.TierPrices.GetOrLoadAsync(Product.Id))
                    .RemoveDuplicatedQuantities();
            }

            return _tierPrices;
        }

        /// <summary>
        /// Gets the product attribute values pre-selected by the merchant.
        /// </summary>
        public async Task<List<ProductVariantAttributeValue>> GetPreSelectedAttributeValuesAsync()
        {
            if (_preSelectedAttributeValues == null)
            {
                var attributes = await Options.BatchContext.Attributes.GetOrLoadAsync(Product.Id);
                var preSelectedValues = attributes.SelectMany(x => x.ProductVariantAttributeValues);

                // Ignore attributes that are filtered out for a bundle item.
                if (BundleItem?.Item?.FilterAttributes ?? false)
                {
                    preSelectedValues = preSelectedValues
                        .Where(x => BundleItem.Item.AttributeFilters.Any(af => af.IsPreSelected && af.AttributeId == x.ProductVariantAttributeId && af.AttributeValueId == x.Id));
                }
                else
                {
                    preSelectedValues = preSelectedValues.Where(x => x.IsPreSelected);
                }

                _preSelectedAttributeValues = preSelectedValues.ToList();
            }

            return _preSelectedAttributeValues;
        }
    }
}