using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a price saving in relation to the calculated final price of a product.
    /// The saving results from the difference between the <see cref="CalculatedPrice.FinalPrice"/> and the <see cref="Product.OldPrice"/> 
    /// or, if present, the <see cref="CalculatedPrice.FinalPriceWithoutDiscount"/>.
    /// </summary>
    public partial class PriceSaving
    {
        public PriceSaving(CalculatedPrice price)
        {
            Guard.NotNull(price, nameof(price));

            // Final price (discounted) has priority over the old price.
            // Avoids differing percentage discount in product lists and detail page.
            SavingPrice = price.FinalPrice < price.FinalPriceWithoutDiscount ? price.FinalPriceWithoutDiscount : price.OldPrice;

            HasSavings = SavingPrice > 0 && price.FinalPrice < SavingPrice;

            if (HasSavings)
            {
                SavingPercent = (float)((SavingPrice - price.FinalPrice) / SavingPrice) * 100;
                SavingAmount = (SavingPrice - price.FinalPrice).WithPostFormat(null);
            }
        }

        /// <summary>
        /// A value indicating whether there is a price saving on the calculated final price.
        /// </summary>
        public bool HasSavings { get; private set; }

        /// <summary>
        /// The price that represents the saving. Often displayed as a crossed-out price.
        /// Always greater than the final price if <see cref="HasSavings"/> is <c>true</c>.
        /// </summary>
        public Money SavingPrice { get; private set; }

        /// <summary>
        /// The saving, in percent, compared to the final price.
        /// </summary>
        public float SavingPercent { get; private set; }

        /// <summary>
        /// The saving, as money amount, compared to the final price.
        /// </summary>
        public Money? SavingAmount { get; private set; }
    }
}
