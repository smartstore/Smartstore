using System.Collections.Generic;
using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a calculated shopping cart subtotal.
    /// </summary>
    public partial class ShoppingCartSubTotal
    {
        public static implicit operator decimal(ShoppingCartSubTotal obj)
            => obj.SubTotalWithDiscount;

        /// <summary>
        /// Cart subtotal excluding discount.
        /// </summary>
        public decimal SubTotalWithoutDiscount { get; set; }

        /// <summary>
        /// Cart subtotal including discount.
        /// </summary>
        public decimal SubTotalWithDiscount { get; set; }

        /// <summary>
        /// Discount amount.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Applied discount.
        /// </summary>
        public Discount AppliedDiscount { get; set; }

        /// <summary>
        /// Tax rates.
        /// </summary>
        public SortedDictionary<decimal, decimal> TaxRates { get; init; } = new();

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="SubTotalWithDiscount"/>.
        /// </summary>
        public override string ToString()
            => SubTotalWithDiscount.FormatInvariant();

        /// <summary>
        /// Adds a tax rate and the related tax amount.
        /// </summary>
        /// <param name="taxRate">Tax rate.</param>
        /// <param name="taxAmount">Tax amount.</param>
        public void AddTaxRate(decimal taxRate, decimal taxAmount)
        {
            if (taxRate > decimal.Zero && taxAmount > decimal.Zero)
            {
                if (TaxRates.ContainsKey(taxRate))
                {
                    TaxRates[taxRate] = TaxRates[taxRate] + taxAmount;
                }
                else
                {
                    TaxRates.Add(taxRate, taxAmount);
                }
            }
        }
    }
}
