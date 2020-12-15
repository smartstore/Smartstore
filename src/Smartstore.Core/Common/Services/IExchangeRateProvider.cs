using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Common;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// Exchange rate provider interface
    /// </summary>
    // TODO: (MH) (core) Apply IProvider to IExchangeRateProvider once finished
    public partial interface IExchangeRateProvider //: IProvider
    {
        /// <summary>
        /// Gets currency live rates
        /// </summary>
        /// <param name="exchangeRateCurrencyCode">Exchange rate currency code</param>
        /// <returns>Exchange rates</returns>
        Task<IList<ExchangeRate>> GetCurrencyLiveRatesAsync(string exchangeRateCurrencyCode);
    }
}