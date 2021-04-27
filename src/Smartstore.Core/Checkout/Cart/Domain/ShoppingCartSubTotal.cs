using System.Collections.Generic;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a calculated shopping cart subtotal.
    /// </summary>
    public partial class ShoppingCartSubTotal
    {
        public static implicit operator Money(ShoppingCartSubTotal obj)
            => obj.SubTotalWithDiscount;

        /// <summary>
        /// Cart subtotal excluding discount in the primary currency.
        /// </summary>
        public Money SubTotalWithoutDiscount { get; init; }

        /// <summary>
        /// Cart subtotal including discount in the primary currency.
        /// </summary>
        public Money SubTotalWithDiscount { get; init; }

        /// <summary>
        /// Discount amount in the primary currency.
        /// </summary>
        public Money DiscountAmount { get; init; }

        /// <summary>
        /// Applied discount.
        /// </summary>
        public Discount AppliedDiscount { get; init; }

        /// <summary>
        /// Tax rates.
        /// </summary>
        public TaxRatesDictionary TaxRates { get; init; }

        // TODO: (mg) (core) describe ShoppingCartLineItem when ready.
        // bundle items are never included in line items.
        public List<ShoppingCartLineItem> LineItems { get; init; }

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="SubTotalWithDiscount"/>.
        /// </summary>
        public override string ToString()
            => SubTotalWithDiscount.ToString();
    }

    public partial class TaxRatesDictionary : SortedDictionary<decimal, decimal>
    {
        /// <summary>
        /// Adds a tax rate and the related tax amount.
        /// </summary>
        /// <param name="taxRate">Tax rate.</param>
        /// <param name="taxAmount">Tax amount.</param>
        public new void Add(decimal taxRate, decimal taxAmount)
        {
            if (taxRate > decimal.Zero && taxAmount > decimal.Zero)
            {
                if (ContainsKey(taxRate))
                {
                    this[taxRate] = this[taxRate] + taxAmount;
                }
                else
                {
                    base.Add(taxRate, taxAmount);
                }
            }
        }
    }
}
