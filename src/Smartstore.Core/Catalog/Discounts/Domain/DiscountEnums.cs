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
}
