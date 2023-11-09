namespace Smartstore.Core.Catalog.Pricing
{
    public enum PricingType
    {
        /// <summary>
        /// Price calculated by price calculation.
        /// </summary>
        Calculated = 0,

        /// <summary>
        /// Customer has to call for price.
        /// </summary>
        CallForPrice,

        /// <summary>
        /// Customer enters the price.
        /// </summary>
        CustomerEnteredPrice
    }

    /// <summary>
    /// Represents pricing targets.
    /// </summary>
    public enum PricingTarget
    {
        /// <summary>
        /// Pricing target is a product
        /// </summary>
        Product,

        /// <summary>
        /// Pricing target is a shipping method charge
        /// </summary>
        ShippingCharge,

        /// <summary>
        /// Pricing target is a payment method fee.
        /// </summary>
        PaymentFee
    }

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