using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a calculated shopping cart shipping total.
    /// </summary>
    public partial class ShoppingCartShippingTotal
    {
        public ShoppingCartShippingTotal(Money? shippingTotal)
        {
            ShippingTotal = shippingTotal;
        }

        public static implicit operator Money?(ShoppingCartShippingTotal obj)
            => obj.ShippingTotal;

        public static implicit operator ShoppingCartShippingTotal(Money? obj)
            => new(obj);

        /// <summary>
        /// Cart shipping total.
        /// </summary>
        public Money? ShippingTotal { get; private set; }

        /// <summary>
        /// Applied discount.
        /// </summary>
        public Discount AppliedDiscount { get; set; }

        /// <summary>
        /// Tax rate.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="SubTotalWithDiscount"/>.
        /// </summary>
        public override string ToString()
            => ShippingTotal.HasValue ? ShippingTotal.ToString() : decimal.Zero.FormatInvariant();
    }
}
