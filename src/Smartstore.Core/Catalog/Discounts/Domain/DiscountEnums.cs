namespace Smartstore.Core.Catalog.Discounts
{
    /// <summary>
    /// Represents a discount type.
    /// </summary>
    public enum DiscountType
    {
        /// <summary>
        /// Assigned to order total .
        /// </summary>
        AssignedToOrderTotal = 1,

        /// <summary>
        /// Assigned to products (SKUs).
        /// </summary>
        AssignedToSkus = 2,

        /// <summary>
		/// Assigned to categories (all products in a category).
        /// </summary>
        AssignedToCategories = 5,

        /// <summary>
        /// Assigned to manufacturers (all products of a manufacturer).
        /// </summary>
        AssignedToManufacturers = 6,

        /// <summary>
        /// Assigned to shipping.
        /// </summary>
        AssignedToShipping = 10,

        /// <summary>
        /// Assigned to order subtotal.
        /// </summary>
        AssignedToOrderSubTotal = 20
    }

    /// <summary>
    /// Represents a discount limitation type.
    /// </summary>
    public enum DiscountLimitationType
    {
        /// <summary>
        /// No limitation.
        /// </summary>
        Unlimited = 0,

        /// <summary>
        /// N times only.
        /// </summary>
        NTimesOnly = 15,

        /// <summary>
        /// N times per customer.
        /// </summary>
        NTimesPerCustomer = 25
    }

    /// <summary>
    /// Allows to specify which discount requirement should be validated.
    /// </summary>
    [Flags]
    public enum DiscountValidationFlags
    {
        None = 0,

        /// <summary>
        /// Validates <see cref="Discount.DiscountLimitation"/>.
        /// </summary>
        DiscountLimitations = 1 << 0,

        /// <summary>
        /// Checks the shopping cart for the existence of gift cards.
        /// No discount is applied if the cart contains gift cards because the customer could earn money through that.
        /// </summary>
        GiftCards = 1 << 1,

        /// <summary>
        /// Validates cart rules.
        /// </summary>
        CartRules = 1 << 2,

        /// <summary>
        /// Validates all discount requirements.
        /// </summary>
        All = DiscountLimitations | GiftCards | CartRules
    }
}
