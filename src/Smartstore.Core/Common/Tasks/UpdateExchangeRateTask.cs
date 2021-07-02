using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Scheduling;

namespace Smartstore.Core.Common.Tasks
{
    /// <summary>
    /// A task that periodically updates exchange rates.
    /// </summary>
    public partial class UpdateExchangeRateTask : ITask
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;

        public UpdateExchangeRateTask(
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            SmartDbContext db,
            IStoreContext storeContext)
        {
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _db = db;
            _storeContext = storeContext;
        }

		public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            if (!_currencySettings.AutoUpdateEnabled)
                return;

            var exchangeRates = await _currencyService
                .GetCurrencyLiveRatesAsync(_storeContext.CurrentStore.PrimaryExchangeRateCurrency.CurrencyCode);

            var currencyCodes = exchangeRates.Select(x => x.CurrencyCode).Distinct().ToArray();
            var currencies = await _db.Currencies
                .Where(x => currencyCodes.Contains(x.CurrencyCode))
                .ToDictionaryAsync(x => x.CurrencyCode, StringComparer.OrdinalIgnoreCase, cancelToken);

            foreach (var exchageRate in exchangeRates)
            {
                if (currencies.TryGetValue(exchageRate.CurrencyCode, out var currency) && currency.Rate != exchageRate.Rate)
                {
                    currency.Rate = exchageRate.Rate;
                }
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}
