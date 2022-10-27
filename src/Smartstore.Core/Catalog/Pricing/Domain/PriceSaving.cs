using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a price saving in relation to the calculated final price of a product.
    /// The saving results from the applied discounts, if any, otherwise from the difference to the <see cref="Product.ComparePrice"/>.
    /// </summary>
    public readonly struct PriceSaving
    {
        /// <summary>
        /// A value indicating whether there is a price saving on the calculated final price.
        /// </summary>
        public bool HasSaving { get; init; }

        /// <summary>
        /// The price that represents the saving. Often displayed as a crossed-out price.
        /// Always greater than the final price if <see cref="HasSaving"/> is <c>true</c>.
        /// </summary>
        public Money SavingPrice { get; init; }

        /// <summary>
        /// The saving, in percent, compared to the final price.
        /// </summary>
        public float SavingPercent { get; init; }

        /// <summary>
        /// The saving, as money amount, compared to the final price.
        /// </summary>
        public Money? SavingAmount { get; init; }
    }
}
