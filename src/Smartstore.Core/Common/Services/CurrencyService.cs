using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Common.Services
{
    public partial class CurrencyService : ICurrencyService
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizationService _localizationService;
        private readonly IProviderManager _providerManager;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly CurrencySettings _currencySettings;
        private readonly TaxSettings _taxSettings;

        public CurrencyService(
            SmartDbContext db,
            ILocalizationService localizationService,
            IProviderManager providerManager,
            IWorkContext workContext,
            IStoreContext storeContext,
            CurrencySettings currencySettings,
            TaxSettings taxSettings)
        {
            _db = db;
            _localizationService = localizationService;
            _providerManager = providerManager;
            _workContext = workContext;
            _storeContext = storeContext;
            _currencySettings = currencySettings;
            _taxSettings = taxSettings;
        }

        public virtual async Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode)
        {
            var exchangeRateProvider = LoadActiveExchangeRateProvider();
            if (exchangeRateProvider != null)
            {
                return await exchangeRateProvider.Value.GetCurrencyLiveRatesAsync(exchangeRateCurrencyCode);
            }

            return new List<ExchangeRate>();
        }

        public virtual async Task<List<Currency>> GetAllCurrenciesAsync(bool includeHidden = false, int storeId = 0)
        {
            return await _db.Currencies.ApplyStandardFilter(includeHidden, storeId).ToListAsync();
        }

        public virtual decimal ConvertCurrency(decimal amount, decimal exchangeRate)
        {
            if (amount != decimal.Zero && exchangeRate != decimal.Zero)
                return amount * exchangeRate;

            return decimal.Zero;
        }

        public virtual decimal ConvertCurrency(decimal amount, Currency sourceCurrency, Currency targetCurrency, Store store = null)
        {
            Guard.NotNull(sourceCurrency, nameof(sourceCurrency));
            Guard.NotNull(targetCurrency, nameof(targetCurrency));

            decimal result = amount;
            if (sourceCurrency.Id == targetCurrency.Id)
                return result;

            if (result != decimal.Zero && sourceCurrency.Id != targetCurrency.Id)
            {
                result = ConvertToPrimaryExchangeRateCurrency(result, sourceCurrency, store);
                result = ConvertFromPrimaryExchangeRateCurrency(result, targetCurrency, store);
            }

            return result;
        }

        public virtual decimal ConvertToPrimaryExchangeRateCurrency(decimal amount, Currency sourceCurrency, Store store = null)
        {
            Guard.NotNull(sourceCurrency, nameof(sourceCurrency));

            decimal result = amount;
            var primaryExchangeRateCurrency = store == null ? _storeContext.CurrentStore.PrimaryExchangeRateCurrency : store.PrimaryExchangeRateCurrency;

            if (result != decimal.Zero && sourceCurrency.Id != primaryExchangeRateCurrency.Id)
            {
                decimal exchangeRate = sourceCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                    throw new SmartException(string.Format("Exchange rate not found for currency [{0}]", sourceCurrency.Name));

                result /= exchangeRate;
            }
            return result;
        }

        public virtual decimal ConvertFromPrimaryExchangeRateCurrency(decimal amount, Currency targetCurrency, Store store = null)
        {
            Guard.NotNull(targetCurrency, nameof(targetCurrency));

            decimal result = amount;
            var primaryExchangeRateCurrency = store == null ? _storeContext.CurrentStore.PrimaryExchangeRateCurrency : store.PrimaryExchangeRateCurrency;

            if (result != decimal.Zero && targetCurrency.Id != primaryExchangeRateCurrency.Id)
            {
                decimal exchangeRate = targetCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                    throw new SmartException(string.Format("Exchange rate not found for currency [{0}]", targetCurrency.Name));

                result *= exchangeRate;
            }
            return result;
        }

        public virtual decimal ConvertToPrimaryStoreCurrency(decimal amount, Currency sourceCurrency, Store store = null)
        {
            Guard.NotNull(sourceCurrency, nameof(sourceCurrency));

            decimal result = amount;
            var primaryStoreCurrency = store == null ? _storeContext.CurrentStore.PrimaryStoreCurrency : store.PrimaryStoreCurrency;

            if (result != decimal.Zero && sourceCurrency.Id != primaryStoreCurrency.Id)
            {
                decimal exchangeRate = sourceCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                    throw new SmartException(string.Format("Exchange rate not found for currency [{0}]", sourceCurrency.Name));

                result /= exchangeRate;
            }
            return result;
        }

        public virtual decimal ConvertFromPrimaryStoreCurrency(decimal amount, Currency targetCurrency, Store store = null)
        {
            Guard.NotNull(targetCurrency, nameof(targetCurrency));

            var primaryStoreCurrency = store == null ? _storeContext.CurrentStore.PrimaryStoreCurrency : store.PrimaryStoreCurrency;
            return ConvertCurrency(amount, primaryStoreCurrency, targetCurrency, store);
        }

        public virtual Provider<IExchangeRateProvider> LoadActiveExchangeRateProvider()
        {
            return LoadExchangeRateProviderBySystemName(_currencySettings.ActiveExchangeRateProviderSystemName) ?? LoadAllExchangeRateProviders().FirstOrDefault();
        }

        public virtual Provider<IExchangeRateProvider> LoadExchangeRateProviderBySystemName(string systemName)
        {
            return _providerManager.GetProvider<IExchangeRateProvider>(systemName);
        }

        public virtual IEnumerable<Provider<IExchangeRateProvider>> LoadAllExchangeRateProviders()
        {
            return _providerManager.GetAllProviders<IExchangeRateProvider>();
        }

        public virtual Money CreateMoney(
            decimal price,
            bool displayCurrency = true,
            object currencyCodeOrObj = null,
            Language language = null,
            bool? priceIncludesTax = null,
            bool? displayTax = null,
            PricingTarget target = PricingTarget.Product)
        {
            Currency currency = null;
            string taxSuffixFormatString = null;

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

                string resource = _localizationService.GetResource(priceIncludesTax.Value ? "Products.InclTaxSuffix" : "Products.ExclTaxSuffix", language.Id, false);
                taxSuffixFormatString = resource.NullEmpty() ?? (priceIncludesTax.Value ? "{0} incl. tax" : "{0} excl. tax");
            }

            return new Money(price, currency)
            {
                ShowTax = displayTax.Value,
                TaxSuffixFormatString = taxSuffixFormatString
            };
        }
    }
}