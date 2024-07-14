﻿using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Scheduling;

namespace Smartstore.Core.Common.Tasks
{
    /// <summary>
    /// A task that periodically updates exchange rates.
    /// </summary>
    public partial class UpdateExchangeRateTask(
        SmartDbContext db,
        ICurrencyService currencyService,
        CurrencySettings currencySettings) : ITask
    {
        private readonly SmartDbContext _db = db;
        private readonly ICurrencyService _currencyService = currencyService;
        private readonly CurrencySettings _currencySettings = currencySettings;

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            if (!_currencySettings.AutoUpdateEnabled)
            {
                return;
            }

            var exchangeRates = await _currencyService.GetCurrencyLiveRatesAsync();

            var currencyCodes = exchangeRates.ToDistinctArray(x => x.CurrencyCode);
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
