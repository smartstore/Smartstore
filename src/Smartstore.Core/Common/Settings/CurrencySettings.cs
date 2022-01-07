using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Settings
{
    /// <summary>
    /// Global currency settings
    /// </summary>
    public class CurrencySettings : ISettings
    {
        /// <summary>
        /// Primary currency identifier
        /// </summary>
        public int PrimaryCurrencyId { get; set; } // TODO: (core) PrimaryCurrencyId. Migrate new setting somehow.

        /// <summary>
        ///  Primary exchange rate currency identifier
        /// </summary>
        public int PrimaryExchangeCurrencyId { get; set; }  // TODO: (core) PrimaryExchangeRateCurrencyId. Migrate new setting somehow.

        /// <summary>
        /// Active exchange rate provider system name
        /// </summary>
        public string ActiveExchangeRateProviderSystemName { get; set; }

        /// <summary>
        /// Whether automatic currency rate update is enabled
        /// </summary>
        public bool AutoUpdateEnabled { get; set; }
    }
}
