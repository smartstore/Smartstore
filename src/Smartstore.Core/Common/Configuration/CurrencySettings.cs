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

        /// <summary>
        /// Gets or sets the default rule for rounding during shopping cart calculation.
        /// </summary>
        public CartRoundingRule RoundCartRule { get; set; }

        /// <summary>
        /// Gets or sets the midpoint rounding strategy.
        /// </summary>
        /// <remarks>ASP.NET Core default value is <see cref="MidpointRounding.ToEven"/>.</remarks>
        public MidpointRounding MidpointRounding { get; set; } = MidpointRounding.ToEven;
    }
}
