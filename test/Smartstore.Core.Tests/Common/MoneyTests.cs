using System;
using NUnit.Framework;
using Smartstore.Core.Common;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Common
{
    [TestFixture]
    public class MoneyTests
    {
        Currency currencyUSD, currencyRUR, currencyEUR;

        [SetUp]
        public void SetUp()
        {
            currencyUSD = new Currency
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
            currencyEUR = new Currency
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
            currencyRUR = new Currency
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
        }

        [Test]
        public void Can_convert_currency_1()
        {
            currencyUSD.AsMoney(10.1M).Exchange(1.5M).Amount.ShouldEqual(15.15M);
            currencyUSD.AsMoney(10.1M).Exchange(1).Amount.ShouldEqual(10.1M);
            currencyUSD.AsMoney(10.1M).Exchange(0).Amount.ShouldEqual(0);
            currencyUSD.AsMoney(0).Exchange(5).Amount.ShouldEqual(0);
        }

        [Test]
        public void Can_convert_currency_2()
        {
            currencyEUR.AsMoney(10M).ExchangeTo(currencyRUR, currencyEUR).Amount.ShouldEqual(345M);
            currencyEUR.AsMoney(10.1M).ExchangeTo(currencyEUR, currencyEUR).Amount.ShouldEqual(10.1M);
            currencyRUR.AsMoney(10.1M).ExchangeTo(currencyRUR, currencyEUR).Amount.ShouldEqual(10.1M);
            currencyUSD.AsMoney(12M).ExchangeTo(currencyRUR, currencyEUR).Amount.ShouldEqual(345M);
            currencyRUR.AsMoney(345M).ExchangeTo(currencyUSD, currencyEUR).Amount.ShouldEqual(12M);
        }
    }
}
