using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Defines options and settings that affect the way how the price calculation pipeline operates.
    /// </summary>
    public class PriceCalculationOptions : ICloneable<PriceCalculationOptions>
    {
        private ProductBatchContext _batchContext;
        private Customer _customer;
        private Store _store;
        private Language _language;
        private Currency _targetCurrency;

        /// <summary>
        /// Creates a new options instance.
        /// </summary>
        /// <param name="batchContext">The product batch context to use. Required.</param>
        /// <param name="customer">The current customer. Required.</param>
        /// <param name="store">The current store. Required.</param>
        /// <param name="language">The working language. Required.</param>
        /// <param name="targetCurrency">The target currency for money exchange. Required.</param>
        public PriceCalculationOptions(ProductBatchContext batchContext, Customer customer, Store store, Language language, Currency targetCurrency)
        {
            Guard.NotNull(batchContext);
            Guard.NotNull(customer);
            Guard.NotNull(store);
            Guard.NotNull(language);
            Guard.NotNull(targetCurrency);

            BatchContext = batchContext;
            Customer = customer;
            Store = store;
            Language = language;
            TargetCurrency = targetCurrency;
            RoundingCurrency = targetCurrency;
        }

        /// <summary>
        /// Gets or sets the product batch context.
        /// </summary>
        public ProductBatchContext BatchContext
        {
            get => _batchContext;
            set => _batchContext = value ?? throw new ArgumentNullException(nameof(BatchContext));
        }

        /// <summary>
        /// Gets or sets the current customer.
        /// </summary>
        public Customer Customer
        {
            get => _customer;
            set => _customer = value ?? throw new ArgumentNullException(nameof(Customer));
        }

        /// <summary>
        /// Gets or sets the current store.
        /// </summary>
        public Store Store
        {
            get => _store;
            set => _store = value ?? throw new ArgumentNullException(nameof(Store));
        }

        /// <summary>
        /// Gets or sets the working language.
        /// </summary>
        public Language Language
        {
            get => _language;
            set => _language = value ?? throw new ArgumentNullException(nameof(Language));
        }

        /// <summary>
        /// Gets or sets the target currency to use for money exchange after a pipeline has been invoked.
        /// </summary>
        public Currency TargetCurrency
        {
            get => _targetCurrency;
            set => _targetCurrency = value ?? throw new ArgumentNullException(nameof(TargetCurrency));
        }

        /// <summary>
        /// Gets or sets the currency used for rounding.
        /// Generally this is the working currency (default), Order.CustomerCurrencyCode (for existing orders) 
        /// or DataExporterContext.ContextCurrency (for exports).
        /// </summary>
        public Currency RoundingCurrency { get; init; }

        /// <summary>
        /// Gets or sets product batch context for nested pipelines (grouped or bundled products).
        /// </summary>
        public ProductBatchContext ChildProductsBatchContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to apply product attributes preselected by merchant in price calculation.
        /// </summary>
        public bool ApplyPreselectedAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an offer price should be ignored during price calculation.
        /// </summary>
        public bool IgnoreOfferPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tier prices should be ignored during price calculation.
        /// </summary>
        public bool IgnoreTierPrices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether percentage discounts on tier prices should be ignored during price calculation.
        /// </summary>
        public bool IgnorePercentageDiscountOnTierPrices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore percentage tier prices on attribute price adjustments.
        /// </summary>
        public bool IgnorePercentageTierPricesOnAttributePriceAdjustments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether discounts should be ignored during price calculation.
        /// </summary>
        public bool IgnoreDiscounts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the source price (the one retrieved from database) includes sales tax already.
        /// </summary>
        public bool IsGrossPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether resulting monetary amounts (after the pipeline has been invoked) should include sales taxes.
        /// </summary>
        public bool TaxInclusive { get; set; }

        /// <summary>
        /// Gets or sets the optional tax format string (e.g. "{0} *", "{0} incl. tax")
        /// </summary>
        public string TaxFormat { get; set; }

        /// <summary>
        /// Gets or sets the optional price range format string (e.g. "from {0}", "ab {0}")
        /// </summary>
        public string PriceRangeFormat { get; set; }

        /// <summary>
        /// Gets or sets flags for discount requirements to be validated.
        /// </summary>
        /// <remarks>
        /// Turning this to <see cref="DiscountValidationFlags.All"/> in larger listings can have heavy impact on calculation performance!
        /// If set to <see cref="DiscountValidationFlags.None"/> then some discount requirements (such as
        /// usage history limitation or rule fulfilment) are ignored during price calculation.
        /// Coupon codes and date ranges are always validated.
        /// </remarks>
        public DiscountValidationFlags DiscountValidationFlags { get; set; } = DiscountValidationFlags.All;
        //public bool CheckDiscountValidity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the lowest possible price should be determined
        /// (cheapest child product, cheapest attribute combination, or lowest tier price).
        /// Turn this on to show price ranges in listings. May have impact on performance.
        /// </summary>
        public bool DetermineLowestPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to determine the price that is initially displayed on the product detail page.
        /// (applies the attribute combination price determined by the attributes preselected by the merchant).
        /// May have impact on performance.
        /// </summary>
        public bool DeterminePreselectedPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to determine price adjustments of product attributes, see <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
        /// Price adjustments are returned through <see cref="CalculatedPrice.AttributePriceAdjustments"/>.
        /// </summary>
        public bool DeterminePriceAdjustments { get; set; }

        /// <inheritdoc/>
        public PriceCalculationOptions Clone()
            => ((ICloneable)this).Clone() as PriceCalculationOptions;

        /// <inheritdoc/>
        object ICloneable.Clone()
            => MemberwiseClone();
    }
}
