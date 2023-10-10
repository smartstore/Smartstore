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

        /// <summary>
        /// Gets or sets a rounding difference amount (in the smallest currency unit, e.g. cents) up to which the order total should be adjusted.
        /// 0 to not adjust the order total.
        /// A difference (typically 1 cent) may occur if there is no rounding during shopping cart calculation where on the other hand prices are always displayed rounded.
        /// </summary>
        /// <remarks>
        /// Works similar to <see cref="Currency.RoundOrderTotalEnabled"/> with the difference that this total adjustment takes place silently.
        /// The adjusted amount is not displayed on the shopping cart page.
        /// This way the admin is not forced to activate the rounding during shopping cart calculation to avoid rounding differences.
        /// </remarks>
        public int RoundOrderTotalDifference { get; set; } = 2;

        #endregion
    }
}
