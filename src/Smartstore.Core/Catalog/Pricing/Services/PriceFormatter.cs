using System;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceFormatter : IPriceFormatter
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly TaxSettings _taxSettings;

        public PriceFormatter(
            SmartDbContext db,
            IWorkContext workContext,
            ILocalizationService localizationService,
            TaxSettings taxSettings)
        {
            _db = db;
            _workContext = workContext;
            _localizationService = localizationService;
            _taxSettings = taxSettings;
        }

        public virtual string FormatPrice(
            decimal price, 
            bool displayCurrency = true, 
            object currencyCodeOrObj = null, 
            Language language = null, 
            bool? priceIncludesTax = null, 
            bool? displayTax = null,
            PricingTarget target = PricingTarget.Product)
        {
            Currency currency = null;
            
            if (currencyCodeOrObj is null)
            {
                currency = _workContext.WorkingCurrency;
            }
            else if (currencyCodeOrObj is string currencyCode)
            {
                Guard.NotEmpty(currencyCode, nameof(currencyCodeOrObj));
                currency = _db.Currencies.FirstOrDefault(x => x.CurrencyCode == currencyCode) ?? new Currency { CurrencyCode = currencyCode };
            }
            else if (currencyCodeOrObj is Currency)
            {
                currency = (Currency)currencyCodeOrObj;
            }

            if (currency == null)
            {
                throw new ArgumentException("Currency parameter must either be a valid currency code as string or an actual currency entity instance.", nameof(currencyCodeOrObj));
            }

            var formatted = new Money(price, currency).ToString(displayCurrency);

            displayTax ??= target == PricingTarget.Product 
                ? _taxSettings.DisplayTaxSuffix
                : (target == PricingTarget.ShippingCharge 
                    ? _taxSettings.DisplayTaxSuffix && _taxSettings.ShippingIsTaxable 
                    : _taxSettings.DisplayTaxSuffix && _taxSettings.PaymentMethodAdditionalFeeIsTaxable);

            if (displayTax == true)
            {
                // Show tax suffix.
                priceIncludesTax ??= _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
                language ??= _workContext.WorkingLanguage;

                var resource = _localizationService.GetResource(priceIncludesTax.Value ? "Products.InclTaxSuffix" : "Products.ExclTaxSuffix", language.Id, false);
                var formatStr = resource.NullEmpty() ?? (priceIncludesTax.Value ? "{0} incl. tax" : "{0} excl. tax");

                formatted = string.Format(formatStr, formatted);
            }

            return formatted;
        }
        
        public virtual string FormatTaxRate(decimal taxRate)
        {
            return taxRate.ToString("G29");
        }

        public virtual string GetBasePriceInfo(Product product, decimal productPrice, Currency currency)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(currency, nameof(currency));

            if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
            {
                var value = Convert.ToDecimal((productPrice / product.BasePriceAmount) * product.BasePriceBaseAmount);
                var valueFormatted = FormatPrice(value, true, currency);
                var amountFormatted = Math.Round(product.BasePriceAmount.Value, 2).ToString("G29");
                var infoTemplate = _localizationService.GetResource("Products.BasePriceInfo");

                var result = infoTemplate.FormatInvariant(
                    amountFormatted,
                    product.BasePriceMeasureUnit,
                    valueFormatted,
                    product.BasePriceBaseAmount
                );

                return result;
            }

            return string.Empty;
        }
    }
}
