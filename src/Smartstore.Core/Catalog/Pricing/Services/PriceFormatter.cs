using System.Threading.Tasks;
using Dasync.Collections;
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

        public virtual async Task<string> FormatPriceAsync(decimal price)
        {
            return await FormatPriceAsync(price, true, _workContext.WorkingCurrency);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency)
        {
            return await FormatPriceAsync(
                price, 
                showCurrency, 
                targetCurrency,
                _workContext.WorkingLanguage,
                _workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, bool showTax)
        {
            return await FormatPriceAsync(
                price, 
                showCurrency,
                _workContext.WorkingCurrency,
                _workContext.WorkingLanguage,
                _workContext.TaxDisplayType == TaxDisplayType.IncludingTax, 
                showTax);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, bool showTax, Language language)
        {
            return await FormatPriceAsync(
                price,
                showCurrency,
                currencyCode,
                language,
                _workContext.TaxDisplayType == TaxDisplayType.IncludingTax,
                showTax);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax)
        {
            return await FormatPriceAsync(
                price,
                showCurrency,
                currencyCode,
                language, 
                priceIncludesTax,
                _taxSettings.DisplayTaxSuffix);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax, bool showTax)
        {
            Guard.NotEmpty(currencyCode, nameof(currencyCode));

            var currency = await _db.Currencies.FirstOrDefaultAsync(x => x.CurrencyCode == currencyCode);

            return await FormatPriceAsync(
                price, 
                showCurrency, 
                currency ?? new Currency { CurrencyCode = currencyCode }, 
                language, 
                priceIncludesTax, 
                showTax);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax)
        {
            return await FormatPriceAsync(
                price, 
                showCurrency, 
                targetCurrency, 
                language, 
                priceIncludesTax, 
                _taxSettings.DisplayTaxSuffix);
        }

        public virtual async Task<string> FormatPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax)
        {
            var formatted = new Money(price, targetCurrency).ToString(showCurrency);

            if (showTax)
            {
                // Show tax suffix.
                var resource = await _localizationService.GetResourceAsync(priceIncludesTax ? "Products.InclTaxSuffix" : "Products.ExclTaxSuffix", language.Id, false);
                var formatStr = resource.NullEmpty() ?? (priceIncludesTax ? "{0} incl. tax" : "{0} excl. tax");

                formatted = string.Format(formatStr, formatted);
            }

            return formatted;
        }


        public virtual async Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency)
        {
            return await FormatShippingPriceAsync(
                price,
                showCurrency,
                _workContext.WorkingCurrency,
                _workContext.WorkingLanguage,
                _workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
        }

        public virtual async Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax)
        {           
            return await FormatPriceAsync(
                price, 
                showCurrency,
                targetCurrency, 
                language,
                priceIncludesTax,
                _taxSettings.ShippingIsTaxable && _taxSettings.DisplayTaxSuffix);
        }

        public virtual async Task<string> FormatShippingPriceAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax, bool showTax)
        {
            return await FormatPriceAsync(
                price, 
                showCurrency, 
                currencyCode, 
                language, 
                priceIncludesTax, 
                showTax);
        }


        public virtual async Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency)
        {
            return await FormatPaymentMethodAdditionalFeeAsync(
                price, 
                showCurrency,
                _workContext.WorkingCurrency,
                _workContext.WorkingLanguage,
                _workContext.TaxDisplayType == TaxDisplayType.IncludingTax);
        }

        public virtual async Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency, Currency targetCurrency, Language language, bool priceIncludesTax)
        {
            return await FormatPriceAsync(
                price,
                showCurrency,
                targetCurrency,
                language,
                priceIncludesTax,
                _taxSettings.PaymentMethodAdditionalFeeIsTaxable && _taxSettings.DisplayTaxSuffix);
        }

        public virtual async Task<string> FormatPaymentMethodAdditionalFeeAsync(decimal price, bool showCurrency, string currencyCode, Language language, bool priceIncludesTax, bool showTax)
        {
            return await FormatPriceAsync(
                price, 
                showCurrency,
                currencyCode, 
                language, 
                priceIncludesTax, 
                showTax);
        }

        
        public virtual string FormatTaxRate(decimal taxRate)
        {
            return taxRate.ToString("G29");
        }
    }
}
