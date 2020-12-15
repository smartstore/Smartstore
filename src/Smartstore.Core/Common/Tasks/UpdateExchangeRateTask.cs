using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Configuration;
using Smartstore.Core.Scheduling;

namespace Smartstore.Core.Common.Services
{
    /// <summary>
    /// A task that periodically updates exchange rates.
    /// </summary>
    public partial class UpdateExchangeRateTask : ITask
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ICommonServices _services;
        
        public UpdateExchangeRateTask(
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            ICommonServices services)
        {
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _services = services;
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
                    .GetCurrencyLiveRatesAsync(_services.StoreContext.CurrentStore.PrimaryExchangeRateCurrency.CurrencyCode);

                foreach (var exchageRate in exchangeRates)
                {
                    var currency = await _services.DbContext.Currencies.
                        ApplyCurrencyCodeFilter(exchageRate.CurrencyCode)
                        .FirstOrDefaultAsync(cancellationToken: cancelToken);

                    if (currency != null && currency.Rate != exchageRate.Rate)
                    {
                        currency.Rate = exchageRate.Rate;
                    }
                }

                // Save new current date as last execution time.
                _currencySettings.LastUpdateTime = DateTime.UtcNow.ToBinary();
                await _services.Settings.ApplySettingAsync(_currencySettings, x => x.LastUpdateTime);

                await _services.DbContext.SaveChangesAsync(cancelToken);
            }
        }
    }
}
