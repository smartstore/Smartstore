using System;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Settings
{
    public class CurrencySettings : ISettings
    {
        public string ActiveExchangeRateProviderSystemName { get; set; }
        public bool AutoUpdateEnabled { get; set; }
        public long LastUpdateTime { get; set; }
    }
}
