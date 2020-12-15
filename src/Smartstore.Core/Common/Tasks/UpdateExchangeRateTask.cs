using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Scheduling;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Common.Services
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
        private readonly ISettingService _settingService;

        public UpdateExchangeRateTask(
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            SmartDbContext db,
            IStoreContext storeContext,
            ISettingService settingService)
        {
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _db = db;
            _storeContext = storeContext;
            _settingService = settingService;
        }

		public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            if (!_currencySettings.AutoUpdateEnabled)
                return;

            long lastUpdateTimeTicks = _currencySettings.LastUpdateTime;
            DateTime lastUpdateTime = DateTime.FromBinary(lastUpdateTimeTicks);
            lastUpdateTime = DateTime.SpecifyKind(lastUpdateTime, DateTimeKind.Utc);

            // Don't update currencies if last execution time is less then an hour in the past.
            if (lastUpdateTime.AddHours(1) < DateTime.UtcNow)
            {
                var exchangeRates = await _currencyService
                    .GetCurrencyLiveRatesAsync(_storeContext.CurrentStore.PrimaryExchangeRateCurrency.CurrencyCode);

                var currencyCodes = exchangeRates.Select(x => x.CurrencyCode).Distinct().ToArray();
                var currencies = await _db.Currencies
                    .Where(x => currencyCodes.Contains(x.CurrencyCode))
                    .ToDictionaryAsync(x => x.CurrencyCode, StringComparer.OrdinalIgnoreCase);

                foreach (var exchageRate in exchangeRates)
                {
                    if (currencies.TryGetValue(exchageRate.CurrencyCode, out var currency) && currency.Rate != exchageRate.Rate)
                    {
                        currency.Rate = exchageRate.Rate;
                    }
                }

                // Save new current date as last execution time.
                _currencySettings.LastUpdateTime = DateTime.UtcNow.ToBinary();
                await _settingService.ApplySettingAsync(_currencySettings, x => x.LastUpdateTime);

                await _db.SaveChangesAsync(cancelToken);
            }
        }
    }
}
