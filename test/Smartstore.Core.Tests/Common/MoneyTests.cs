using System;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Common
{
    [TestFixture]
    public class MoneyTests : ServiceTestBase
    {
        CurrencySettings _currencySettings;
        IRoundingHelper _roundingHelper;
        ICurrencyService _currencyService;
        Currency _currencyUSD, _currencyRUR, _currencyEUR;

        [SetUp]
        public new void SetUp()
        {
            _currencyUSD = new Currency
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
            _currencyEUR = new Currency
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
            _currencyRUR = new Currency
            {
                Id = 3,
                Name = "Russian Rouble",
                CurrencyCode = "RUB",
                Rate = 34.5M,
                DisplayLocale = "ru-RU",
                CustomFormatting = "",
                Published = true,
                DisplayOrder = 3,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };

            _currencySettings = new CurrencySettings();
            _roundingHelper = new RoundingHelper(new Mock<IWorkContext>().Object, _currencySettings);

            _currencyService = new CurrencyService(
                DbContext,
                NullCache.Instance,
                ProviderManager,
                null,
                _currencySettings,
                null,
                _roundingHelper)
            {
                PrimaryCurrency = _currencyUSD,
                PrimaryExchangeCurrency = _currencyEUR
            };
        }

        [Test]
        public void Can_convert_currency_1()
        {
            _currencyService.CreateMoney(10.1M, _currencyUSD).Exchange(1.5M).Amount.ShouldEqual(15.15M);
            _currencyService.CreateMoney(10.1M, _currencyUSD).Exchange(1).Amount.ShouldEqual(10.1M);
            _currencyService.CreateMoney(10.1M, _currencyUSD).Exchange(0).Amount.ShouldEqual(0);
            _currencyService.CreateMoney(0, _currencyUSD).Exchange(5).Amount.ShouldEqual(0);
        }

        [Test]
        public void Can_convert_currency_2()
        {
            _currencyService.CreateMoney(10M, _currencyEUR).ExchangeTo(_currencyRUR, _currencyEUR).Amount.ShouldEqual(345M);
            _currencyService.CreateMoney(10.1M, _currencyEUR).ExchangeTo(_currencyEUR, _currencyEUR).Amount.ShouldEqual(10.1M);
            _currencyService.CreateMoney(10.1M, _currencyRUR).ExchangeTo(_currencyRUR, _currencyEUR).Amount.ShouldEqual(10.1M);
            _currencyService.CreateMoney(12M, _currencyUSD).ExchangeTo(_currencyRUR, _currencyEUR).Amount.ShouldEqual(345M);
            _currencyService.CreateMoney(345M, _currencyRUR).ExchangeTo(_currencyUSD, _currencyEUR).Amount.ShouldEqual(12M);
        }
    }
}
