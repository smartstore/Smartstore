using System.Globalization;
using System.Net.Http;
using System.Xml;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Gets live currency exchange rates from ECB web service.
    /// </summary>
    [SystemName("CurrencyExchange.ECB")]
    [FriendlyName("ECB currency exchange rate provider")]
    [Order(0)]
    internal class EcbExchangeRateProvider : IExchangeRateProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EcbExchangeRateProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public virtual async Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode)
        {
            Guard.NotEmpty(exchangeRateCurrencyCode);

            var result = new Dictionary<string, ExchangeRate>();
            var targetCurrency = exchangeRateCurrencyCode.ToUpper();
            var updateDate = DateTime.UtcNow;

            var client = _httpClientFactory.CreateClient();

            // Old url still works too: http://www.ecb.int/stats/eurofxref/eurofxref-daily.xml
            using (var stream = await client.GetStreamAsync("http://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml"))
            {
                var document = new XmlDocument();
                document.Load(stream);

                var nsmgr = new XmlNamespaceManager(document.NameTable);
                nsmgr.AddNamespace("ns", "http://www.ecb.int/vocabulary/2002-08-01/eurofxref");
                nsmgr.AddNamespace("gesmes", "http://www.gesmes.org/xml/2002-08-01");

                var node = document.SelectSingleNode("gesmes:Envelope/ns:Cube/ns:Cube", nsmgr);
                var ecbDate = DateTime.ParseExact(node.Attributes["time"].Value, "yyyy-MM-dd", null);

                // See https://www.ecb.europa.eu/stats/exchange/eurofxref/html/index.en.html
                updateDate = new DateTime(ecbDate.Year, ecbDate.Month, ecbDate.Day, 16, 0, 0);

                var provider = new NumberFormatInfo
                {
                    NumberDecimalSeparator = ".",
                    NumberGroupSeparator = string.Empty
                };

                foreach (XmlNode childNode in node.ChildNodes)
                {
                    // Parse ECB rate.
                    var rate = new ExchangeRate
                    {
                        CurrencyCode = childNode.Attributes["currency"].Value.EmptyNull().ToUpper(),
                        Rate = decimal.Parse(childNode.Attributes["rate"].Value, provider),
                        UpdatedOn = updateDate
                    };

                    // Add it to result dictionary.
                    if (!result.ContainsKey(rate.CurrencyCode) && rate.Rate != decimal.Zero)
                    {
                        result.Add(rate.CurrencyCode, rate);
                    }
                }
            }

            // If target currency is EUR then we are ready because ECB always returns EUR rates.
            if (targetCurrency != "EUR")
            {
                // Cross calculation not possible if ECB list does not contain target currency.
                if (!result.ContainsKey(targetCurrency))
                {
                    return new List<ExchangeRate>();
                }

                // Get clone of EUR rates.
                var euroRates = result.ToDictionary(x => x.Key, x => x.Value.Rate);

                // Cross calculate rates for target currency.
                result.Each(x =>
                {
                    if (x.Value.CurrencyCode == "EUR")
                    {
                        x.Value.Rate = 1.0M / euroRates[targetCurrency];
                    }
                    else
                    {
                        x.Value.Rate = euroRates[x.Value.CurrencyCode] / euroRates[targetCurrency];
                    }
                });

                // Remove target currency form result because its rate is always 1.0.
                result.Remove(targetCurrency);

                // Add EUR rate.
                if (!result.ContainsKey("EUR"))
                {
                    result.Add("EUR", new()
                    {
                        CurrencyCode = "EUR",
                        Rate = 1.0M / euroRates[targetCurrency],
                        UpdatedOn = updateDate
                    });
                }
            }

            return result.Select(x => x.Value).ToList();
        }
    }
}