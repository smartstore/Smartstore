using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a discount amount calculated during price calculation. Typically used when the discount amount cannot be applied to 
    /// <see cref="CalculatedPrice.FinalPrice"/> directly, but only later during the calculation.
    /// </summary>
    public partial class CalculatedDiscount
    {
        /// <summary>
        /// Origin of the calculated discount amount, e.g. 'MinTierPrice'.
        /// </summary>
        public string Origin { get; set; } = string.Empty;

        /// <summary>
        /// The associated discount entity.
        /// </summary>
        public Discount Discount { get; set; }

        /// <summary>
        /// The calculated discount amount.
        /// </summary>
        public decimal DiscountAmount { get; set; }
    }
}
