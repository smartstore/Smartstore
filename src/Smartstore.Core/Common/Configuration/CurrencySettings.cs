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
        /// Active exchange rate provider system name
        /// </summary>
        public string ActiveExchangeRateProviderSystemName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically update currency rates.
        /// </summary>
        public bool AutoUpdateEnabled { get; set; }

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
        /// </summary>
        public bool RoundUnitPrices { get; set; } = true;

        /// <summary>
        /// Gets or sets the rounding strategy of the midway between two numbers.
        /// </summary>
        /// <remarks>
        /// <see cref="MidpointRounding.ToEven"/> equals "banker's rounding" or "mathematisches Runden".
        /// Round to the nearest even number.
        /// Examples: 1.2250 is rounded down to 1.22. 1.2350 is rounded up to 1.24.
        /// 
        /// <see cref="MidpointRounding.AwayFromZero"/> equals "round to nearest" or "kaufmännisches Runden".
        /// Round down if the first dropped decimal place is 0, 1, 2, 3 or 4, otherwise round up.
        /// Examples: 1.2250 is rounded up to 1.23. 1.2240 is rounded down to 1.22.
        /// </remarks>
        public MidpointRounding MidpointRounding { get; set; } = MidpointRounding.ToEven;

        #endregion
    }
}
