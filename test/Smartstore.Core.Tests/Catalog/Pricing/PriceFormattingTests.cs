using System;
using System.Globalization;
using System.Threading;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Catalog.Pricing
{
    [TestFixture]
    public class PriceFormattingTests : ServiceTestBase
    {
        ITaxService _taxService;
        ILocalizationService _localizationService;
        IWorkContext _workContext;
        TaxSettings _taxSettings;

        Currency _currencyEUR;
        Currency _currencyUSD;

        [SetUp]
        public new void SetUp()
        {
            _taxSettings = new TaxSettings();

            _currencyEUR = new Currency
            {
                Id = 1,
                Name = "Euro",
                CurrencyCode = "EUR",
                DisplayLocale = "",
                CustomFormatting = "€0.00",
                DisplayOrder = 1,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            _currencyUSD = new Currency
            {
                Id = 1,
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
                CustomFormatting = "",
                DisplayOrder = 2,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            var workContextMock = new Mock<IWorkContext>();
            _workContext = workContextMock.Object;

            var localizationServiceMock = new Mock<ILocalizationService>();
            _localizationService = localizationServiceMock.Object;

            _taxService = new TaxService(
                DbContext,
                null,
                ProviderManager,
                _workContext,
                _localizationService,
                _taxSettings);
        }

        [Test]
        public void Can_formatPrice_with_custom_currencyFormatting()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            new Money(1234.5M, _currencyEUR).ToString().ShouldEqual("€1234.50");

            _currencyEUR.CustomFormatting = "€ 0.00";

            new Money(1234.5M, _currencyEUR).ToString().ShouldEqual("€ 1234.50");
        }

        [Test]
        public void Can_formatPrice_with_distinct_currencyDisplayLocale()
        {
            _currencyEUR.CustomFormatting = string.Empty;

            new Money(1234.5M, _currencyUSD).ToString().ShouldEqual("$1,234.50");
            new Money(1234.5M, _currencyEUR).ToString().ShouldEqual("1.234,50 €");
        }

        [Test]
        public void Can_formatPrice_with_showTax()
        {
            var language = new Language
            {
                Id = 1,
                Name = "English",
                LanguageCulture = "en-US"
            };

            var formatIncl = _taxService.GetTaxFormat(true, true, PricingTarget.Product, language);
            var formatExcl = _taxService.GetTaxFormat(true, false, PricingTarget.Product, language);

            new Money(1234.5M, _currencyUSD, false, formatIncl).ToString().ShouldEqual("$1,234.50 incl. tax");
            new Money(1234.5M, _currencyUSD, false, formatExcl).ToString().ShouldEqual("$1,234.50 excl. tax");
        }

        [Test]
        public void Can_formatPrice_with_showCurrencyCode()
        {
            new Money(1234.5M, _currencyUSD, false).ToString().ShouldEqual("$1,234.50");
            new Money(1234.5M, _currencyUSD, true).ToString().ShouldEqual("1,234.50");
        }
    }
}