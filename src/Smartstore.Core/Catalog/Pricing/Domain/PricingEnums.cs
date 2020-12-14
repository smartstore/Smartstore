using System;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents types of product prices to display.
    /// </summary>
    public enum PriceDisplayType
    {
        /// <summary>
        /// The lowest possible price of a product (default)
        /// </summary>
        LowestPrice = 0,

        /// <summary>
        /// The product price initially displayed on the product detail page
        /// </summary>
        PreSelectedPrice = 10,

        /// <summary>
        /// The product price without associated data like discounts, tier prices, attributes or attribute combinations
        /// </summary>
        PriceWithoutDiscountsAndAttributes = 20,

        /// <summary>
        /// Do not display a product price
        /// </summary>
        Hide = 30
    }

    /// <summary>
    /// Represents the style in which prices are displayed.
    /// </summary>
    [Flags]
    public enum PriceDisplayStyle
    {
        /// <summary>
        /// Display prices without badges
        /// </summary>
        Default = 1,

        /// <summary>
        /// Display all prices within badges
        /// </summary>
        BadgeAll = 2,

        /// <summary>
        /// Display prices of free products within badges 
        /// </summary>
        BadgeFreeProductsOnly = 4
    }

    /// <summary>
    /// Represents the tier price calculation method.
    /// </summary>
    public enum TierPriceCalculationMethod
    {
        /// <summary>
        /// Fixed tier price.
        /// </summary>
        Fixed = 0,

        /// <summary>
        /// Percental tier price.
        /// </summary>
        Percental = 5,

        /// <summary>
        /// Adjusted tier price.
        /// </summary>
        Adjustment = 10
    }
}
