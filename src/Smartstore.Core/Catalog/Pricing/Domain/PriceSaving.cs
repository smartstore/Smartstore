using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    // TODO: (mg) (core) Check saving price. Actually undiscounted price required. Former code compared the undiscounted with the discounted final price, not the regular price (can be 0).
    // TODO: (mg) (core) Describe PriceSaving more when ready

    /// <summary>
    /// Represents a price saving in relation to the calculated final price of a product.
    /// </summary>
    public partial class PriceSaving
    {
        public PriceSaving(CalculatedPrice price)
        {
            Guard.NotNull(price, nameof(price));

            // Final price (discounted) has priority over the old price.
            // Avoids differing percentage discount in product lists and detail page.
            SavingPrice = price.FinalPrice < price.RegularPrice ? price.RegularPrice : price.OldPrice;

            HasSavings = SavingPrice > 0m && price.FinalPrice < SavingPrice;

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
        /// It is greater than the final price if <see cref="HasSavings"/> is <c>true</c>.
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
