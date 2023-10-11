namespace Smartstore.Core.Common
{
    /// <summary>
    /// Represents the rounding strategy of the halfway between two currency amounts.
    /// </summary>
    public enum CurrencyMidpointRounding
    {
        /// <summary>
        /// Round the midpoint between two amounts to the nearest number that is away from zero.
        /// Thus down if the first dropped decimal place is 0, 1, 2, 3 or 4, otherwise round up.
        /// Also called "commercial rounding" or "kaufmännisches Runden".
        /// </summary>
        /// <example>
        /// 1.2250 is rounded up to 1.23.
        /// 1.2240 is rounded down to 1.22.
        /// </example>
        AwayFromZero = 0,

        /// <summary>
        /// Round the midpoint between two amounts to the nearest even number.
        /// Also called "banker's rounding" or "mathematisches Runden".
        /// </summary>
        /// <example>
        /// 1.2250 is rounded down to 1.22.
        /// 1.2350 is rounded up to 1.24.
        /// </example>
        ToEven
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
