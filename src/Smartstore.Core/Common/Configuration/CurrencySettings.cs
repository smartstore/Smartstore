using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Configuration
{
    /// <summary>
    /// Global currency settings
    /// </summary>
    public class CurrencySettings : ISettings
    {
        /// <summary>
        /// Primary currency identifier
        /// </summary>
        public int PrimaryCurrencyId { get; set; }

        /// <summary>
        ///  Primary exchange rate currency identifier
        /// </summary>
        public int PrimaryExchangeCurrencyId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically update currency rates.
        /// </summary>
        public bool AutoUpdateEnabled { get; set; }

        /// <summary>
        /// Active exchange rate provider system name
        /// </summary>
        public string ActiveExchangeRateProviderSystemName { get; set; }

        #region Rounding

        /// <summary>
        /// Gets or sets a value indicating whether rounding during shopping cart calculation is enabled.
        /// </summary>
        public bool RoundOrderItemsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to round during shopping cart calculation when net prices are displayed for the customer.
        /// </summary>
        public bool RoundNetPrices { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to round the unit price before or after quantity multiplication.
        /// <c>true</c> to round before and <c>false</c> to round after quantity multiplication.
        /// </summary>
        public bool RoundUnitPrices { get; set; } = true;

        #endregion
    }
}
