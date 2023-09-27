using System;
using System.Linq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Common
{
    [TestFixture]
    public class CurrencyServiceTests : ServiceTestBase
    {
        ICurrencyService _currencyService;
        Currency _currencyUSD, _currencyJPY, _currencyEUR;

        [SetUp]
        public new void SetUp()
        {
            _currencyUSD = new()
            {
                Id = 1,
                Name = "US Dollar",
                CurrencyCode = "USD",
                Rate = 1.2M,
                DisplayLocale = "en-US",
                CustomFormatting = "",
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };
            _currencyEUR = new()
            {
                Id = 2,
                Name = "Euro",
                CurrencyCode = "EUR",
                Rate = 1,
                DisplayLocale = "de-DE",
                CustomFormatting = "€0.00",
                Published = true,
                DisplayOrder = 2,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };
            _currencyJPY = new()
            {
                Id = 3,
                Name = "Japanese yen",
                CurrencyCode = "JPY",
                Rate = 127.367M,
                DisplayLocale = "ja-JP",
                CustomFormatting = "",
                Published = true,
                DisplayOrder = 3,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };

            _currencyService = new CurrencyService(
                DbContext,
                NullCache.Instance,
                ProviderManager,
                null,
                new CurrencySettings(),
                null, 
                null)
            {
                PrimaryCurrency = _currencyUSD,
                PrimaryExchangeCurrency = _currencyEUR
            };
        }

        [Test]
        public void Can_load_exchangeRateProviders()
        {
            var providers = _currencyService.LoadAllExchangeRateProviders();
            providers.ShouldNotBeNull();
            providers.Any().ShouldBeTrue();
        }

        [Test]
        public void Can_load_exchangeRateProvider_by_systemKeyword()
        {
            var provider = _currencyService.LoadExchangeRateProviderBySystemName("CurrencyExchange.TestProvider");
            provider.ShouldNotBeNull();
        }

        [Test]
        public void Can_load_active_exchangeRateProvider()
        {
            var provider = _currencyService.LoadActiveExchangeRateProvider();
            provider.ShouldNotBeNull();
        }

        [Test]
        public void Can_convert_currency()
        {
            _currencyService.PrimaryCurrency = _currencyEUR;
            _currencyService.ConvertFromPrimaryCurrency(10M, _currencyJPY).Amount.ShouldEqual(1273.67M);
            _currencyService.ConvertFromPrimaryCurrency(10.1M, _currencyEUR).Amount.ShouldEqual(10.1M);

            _currencyService.PrimaryCurrency = _currencyJPY;
            _currencyService.ConvertFromPrimaryCurrency(10.1M, _currencyJPY).Amount.ShouldEqual(10.1M);

            _currencyService.PrimaryCurrency = _currencyUSD;
            _currencyService.ConvertFromPrimaryCurrency(12M, _currencyJPY).Amount.ShouldEqual(1273.67M);

            _currencyService.PrimaryCurrency = _currencyJPY;
            _currencyService.ConvertFromPrimaryCurrency(1273.67M, _currencyUSD).Amount.ShouldEqual(12M);
        }
    }
}
