namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents a rule for rounding during shopping cart calculation.
    /// </summary>
    /// <remarks>
    /// Changes to this rule may lead to rounding differences compared to external services, such as payment providers.
    /// </remarks>
    public enum CartRoundingRule
    {
        /// <summary>
        /// Never round during shopping cart calculation.
        /// Nevertheless, a currency amount is displayed rounded according to <see cref="Currency.RoundNumDecimals"/>.
        /// </summary>
        NeverRound = 0,

        /// <summary>
        /// Prices and tax amounts are rounded at item level.
        /// </summary>
        RoundHorizontal = 10,

        /// <summary>
        /// Prices and tax amounts are first added up and then rounded.
        /// </summary>
        RoundVertical = 20,

        /// <summary>
        /// Round at all relevant places during shopping cart calculation.
        /// </summary>
        AlwaysRound = 30
    }

    public enum CartRoundingItem
    {
        ProductUnitAmount,
        ProductUnitTax,
        SubtotalAmount,
        SubtotalTax,
        ShippingAmount,
        RewardPointsAmount,
        TotalAmount,
        // TODO: probably more required....
    }

    /// <summary>
    /// Represents a rule for rounding the order total to the nearest multiple of denomination (cash rounding).
    /// </summary>
    public enum CurrencyRoundingRule
    {
        /// <summary>
        /// E.g. denomination 0.05: 9.225 will round to 9.20
        /// </summary>
        RoundMidpointDown = 0,

        /// <summary>
        /// E.g. denomination 0.05: 9.225 will round to 9.25
        /// </summary>
        RoundMidpointUp,

        /// <summary>
        /// E.g. denomination 0.05: 9.24 will round to 9.20
        /// </summary>
        AlwaysRoundDown,

        /// <summary>
        /// E.g. denomination 0.05: 9.26 will round to 9.30
        /// </summary>
        AlwaysRoundUp
    }
}
