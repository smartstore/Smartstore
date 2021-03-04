using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public virtual Money ConvertCurrency(Money amount, decimal exchangeRate)
        {
            Guard.NotNull(amount, nameof(amount));

            if (amount != decimal.Zero && exchangeRate != decimal.Zero)
            {
                return amount * exchangeRate;
            }

            return new(amount.Currency);
        }

        public virtual Money ConvertCurrency(Money amount, Currency targetCurrency, Store store = null)
        {
            Guard.NotNull(amount, nameof(amount));
            Guard.NotNull(amount.Currency, nameof(amount.Currency));
            Guard.NotNull(targetCurrency, nameof(targetCurrency));

            var sourceCurrency = amount.Currency;

            if (sourceCurrency.Id == targetCurrency.Id)
            {
                return amount;
            }

            if (amount != decimal.Zero)
            {
                var tmp = ConvertToStoreCurrency(true, amount, store);
                tmp.Currency = targetCurrency;

                return ConvertFromStoreCurrency(true, tmp, store);
            }

            return new(amount.Amount, targetCurrency);
        }

        public virtual Money ConvertToStoreCurrency(bool toExchangeRateCurrency, Money amount, Store store = null)
        {
            Guard.NotNull(amount, nameof(amount));
            Guard.NotNull(amount.Currency, nameof(amount.Currency));

            var sourceCurrency = amount.Currency;
            var targetCurrency = toExchangeRateCurrency
                ? store?.PrimaryExchangeRateCurrency ?? _storeContext.CurrentStore.PrimaryExchangeRateCurrency
                : store?.PrimaryStoreCurrency ?? _storeContext.CurrentStore.PrimaryStoreCurrency;

            if (amount != decimal.Zero && sourceCurrency.Id != targetCurrency.Id)
            {
                var exchangeRate = sourceCurrency.Rate;
                if (exchangeRate == decimal.Zero)
                {
                    throw new SmartException($"Exchange rate not found for currency [{sourceCurrency.Name}].");
                }

                return new(amount.Amount / exchangeRate, targetCurrency);
            }

            return new(amount.Amount, targetCurrency);
        }

        public virtual Money ConvertFromStoreCurrency(bool fromExchangeRateCurrency, Money amount, Store store = null)
        {
            Guard.NotNull(amount, nameof(amount));
            Guard.NotNull(amount.Currency, nameof(amount.Currency));

            var sourceCurrency = fromExchangeRateCurrency
                ? store?.PrimaryExchangeRateCurrency ?? _storeContext.CurrentStore.PrimaryExchangeRateCurrency
                : store?.PrimaryStoreCurrency ?? _storeContext.CurrentStore.PrimaryStoreCurrency;
            var targetCurrency = amount.Currency;

            if (fromExchangeRateCurrency)
            {
                if (amount != decimal.Zero && sourceCurrency.Id != targetCurrency.Id)
                {
                    var exchangeRate = targetCurrency.Rate;
                    if (exchangeRate == decimal.Zero)
                    {
                        throw new SmartException($"Exchange rate not found for currency [{targetCurrency.Name}].");
                    }

                    return new(amount.Amount * exchangeRate, targetCurrency);
                }

                return new(amount.Amount, targetCurrency);
            }
            else
            {
                return ConvertCurrency(new(amount.Amount, sourceCurrency), targetCurrency, store);
            }
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