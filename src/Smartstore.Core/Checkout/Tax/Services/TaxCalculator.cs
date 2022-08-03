using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Tax
{
    public class TaxCalculator : ITaxCalculator
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly ITaxService _taxService;
        private readonly TaxSettings _taxSettings;

        public TaxCalculator(SmartDbContext db, IWorkContext workContext, ITaxService taxService, TaxSettings taxSettings)
        {
            _db = db;
            _workContext = workContext;
            _taxService = taxService;
            _taxSettings = taxSettings;
        }

        public virtual Task<Tax> CalculateProductTaxAsync(
            Product product,
            decimal price,
            bool? inclusive = null,
            Customer customer = null,
            Currency currency = null)
        {
            Guard.NotNull(product, nameof(product));

            return CalculateTaxAsync(product, price, _taxSettings.PricesIncludeTax, null, inclusive, customer, currency);
        }

        public virtual async Task<Tax> CalculateCheckoutAttributeTaxAsync(
            CheckoutAttributeValue attributeValue,
            bool? inclusive = null,
            Customer customer = null,
            Currency currency = null)
        {
            Guard.NotNull(attributeValue, nameof(attributeValue));

            await _db.LoadReferenceAsync(attributeValue, x => x.CheckoutAttribute);

            var attribute = attributeValue.CheckoutAttribute;
            if (attribute.IsTaxExempt)
            {
                return new Tax(TaxRate.Zero, 0m, 0m, true, true, currency);
            }

            return await CalculateTaxAsync(null, attributeValue.PriceAdjustment, _taxSettings.PricesIncludeTax, attribute.TaxCategoryId, inclusive, customer, currency);
        }

        public virtual Task<Tax> CalculateShippingTaxAsync(
            decimal price,
            bool? inclusive = null,
            int? taxCategoryId = null,
            Customer customer = null,
            Currency currency = null)
        {
            if (!_taxSettings.ShippingIsTaxable)
            {
                return Task.FromResult(new Tax(TaxRate.Zero, 0m, price, true, true, currency));
            }

            taxCategoryId ??= _taxSettings.ShippingTaxClassId;
            return CalculateTaxAsync(null, price, _taxSettings.ShippingPriceIncludesTax, taxCategoryId.Value, inclusive, customer, currency);
        }

        public virtual Task<Tax> CalculatePaymentFeeTaxAsync(
            decimal price,
            bool? inclusive = null,
            int? taxCategoryId = null,
            Customer customer = null,
            Currency currency = null)
        {
            if (!_taxSettings.PaymentMethodAdditionalFeeIsTaxable)
            {
                return Task.FromResult(new Tax(TaxRate.Zero, 0m, price, true, true, currency));
            }

            taxCategoryId ??= _taxSettings.PaymentMethodAdditionalFeeTaxClassId;
            return CalculateTaxAsync(null, price, _taxSettings.PaymentMethodAdditionalFeeIncludesTax, taxCategoryId.Value, inclusive, customer, currency);
        }

        protected virtual async Task<Tax> CalculateTaxAsync(
            Product product,
            decimal price,
            bool isGrossPrice,
            int? taxCategoryId = null,
            bool? inclusive = null,
            Customer customer = null,
            Currency currency = null)
        {
            // Don't calculate if price is 0.
            if (price == decimal.Zero)
            {
                return Tax.Zero;
            }

            customer ??= _workContext.CurrentCustomer;
            currency ??= _workContext.WorkingCurrency;
            taxCategoryId ??= product?.TaxCategoryId;
            inclusive ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;

            var taxRate = await _taxService.GetTaxRateAsync(product, taxCategoryId, customer);

            var tax = isGrossPrice
                 // Admin: GROSS prices
                 ? CalculateTaxFromGross(price, taxRate, inclusive.Value, currency)
                 // Admin: NET prices
                 : CalculateTaxFromNet(price, taxRate, inclusive.Value, currency);

            return tax;
        }

        public virtual Tax CalculateTaxFromGross(decimal grossPrice, TaxRate rate, bool inclusive, Currency currency = null)
        {
            if (grossPrice == 0)
                return Tax.Zero;

            return new Tax(rate, grossPrice / ((100 + rate.Rate) / 100) * (rate.Rate / 100),
                grossPrice,
                true,
                inclusive,
                currency);
        }

        public virtual Tax CalculateTaxFromNet(decimal netPrice, TaxRate rate, bool inclusive, Currency currency = null)
        {
            if (netPrice == 0)
                return Tax.Zero;

            return new Tax(rate, netPrice * (rate.Rate / 100),
                netPrice,
                false,
                inclusive,
                currency);
        }
    }
}
