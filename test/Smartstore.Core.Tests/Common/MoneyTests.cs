using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        // JSON Serialization vars
        MoneyStjConverter _converter;
        JsonSerializerOptions _jsonOptions;

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

            // JSON Serialization setup
            _converter = new MoneyStjConverter();
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

        #region JSON Serialization

        [Test]
        public void ST_JsonConverter_Write_Should_Serialize_Money_To_String()
        {
            var money = new Money(123.45M, _currencyEUR);
            var expectedJson = $"\"{money.ToString()}\"";

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            _converter.Write(writer, money, _jsonOptions);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            json.ShouldEqual(expectedJson);
        }

        [Test]
        public void ST_JsonConverter_Write_Should_Handle_Zero_Amount()
        {
            var money = new Money(0M, _currencyUSD);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            _converter.Write(writer, money, _jsonOptions);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            json.Contains('0').ShouldBeTrue();
        }

        [Test]
        public void ST_JsonConverter_Write_Should_Handle_HideCurrency()
        {
            var money = new Money(99.99M, _currencyEUR, true);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            _converter.Write(writer, money, _jsonOptions);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());

            json.Contains('€').ShouldBeFalse();
            json.Contains("99").ShouldBeTrue();
        }

        [Test]
        public void ST_JsonConverter_Write_Should_Handle_PostFormat()
        {
            var money = new Money(50M, _currencyUSD, false, "{0} incl. tax");

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            _converter.Write(writer, money, _jsonOptions);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            json.Contains("incl. tax").ShouldBeTrue();
        }

        [Test]
        public void ST_JsonConverter_Read_Should_Throw_NotSupportedException()
        {
            _jsonOptions.Converters.Add(new MoneyStjConverter());

            var json = "\"$123.45\"";

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Money>(json, _jsonOptions));
        }

        [Test]
        public void ST_JsonConverter_Write_Should_Handle_Different_Currencies()
        {
            var moneyRUR = new Money(1000M, _currencyRUR);

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            _converter.Write(writer, moneyRUR, _jsonOptions);
            writer.Flush();

            var json = Encoding.UTF8.GetString(stream.ToArray());
            
            json.Contains('1').ShouldBeTrue();
            json.Contains("000").ShouldBeTrue();
        }

        #endregion
    }
}
