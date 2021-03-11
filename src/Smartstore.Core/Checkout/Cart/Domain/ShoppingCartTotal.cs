using System.Collections.Generic;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents a calculated shopping cart total.
    /// </summary>
    public partial class ShoppingCartTotal
    {
        public static implicit operator Money?(ShoppingCartTotal obj)
            => obj.Total;

        /// <summary>
        /// Total amount of the shopping cart. <c>null</c> if the cart total couldn't be calculated now.
        /// </summary>
        public Money? Total { get; init; }

        /// <summary>
        /// The amount by which the total was rounded, if rounding to the nearest multiple of denomination 
        /// (cash rounding) is activated for the currency.
        /// </summary>
        /// <example>"Schweizer Rappenrundung" of 16.23 -> <see cref="Total"/> = 16.25 and <see cref="ToNearestRounding"/> = 0.02.</example>
        public Money ToNearestRounding { get; init; }

        /// <summary>
        /// Applied discount amount.
        /// </summary>
        public Money DiscountAmount { get; init; }

        /// <summary>
        /// Applied discount.
        /// </summary>
        public Discount AppliedDiscount { get; init; }

        /// <summary>
        /// Reward points to redeem
        /// </summary>
        public int RedeemedRewardPoints { get; init; }

        /// <summary>
        /// Reward points amount to redeem (in primary store currency).
        /// </summary>
        public Money RedeemedRewardPointsAmount { get; init; }

        /// <summary>
        /// Credit balance.
        /// </summary>
        public Money CreditBalance { get; init; }

        /// <summary>
        /// Applied gift cards.
        /// </summary>
        public List<AppliedGiftCard> AppliedGiftCards { get; init; }

        /// <summary>
        /// Total converted from primary store currency.
        /// </summary>
        public ConvertedAmounts ConvertedAmount { get; init; }

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="Total"/>.
        /// </summary>
        public override string ToString()
            => Total?.ToString() ?? decimal.Zero.FormatInvariant();

        /// <summary>
        /// Represents converted amount of <see cref="ShoppingCartTotal.Total"/> and <see cref="ShoppingCartTotal.ToNearestRounding"/>.
        /// </summary>
        public class ConvertedAmounts
        {
            /// <summary>
            /// Converted shopping cart total amount. <c>null</c> if the cart total couldn't be calculated now.
            /// </summary>
            public Money? Total { get; init; }

            /// <summary>
            /// Converted amount by which the total was rounded, if rounding to the nearest multiple of denomination 
            /// (cash rounding) is activated for the currency.
            /// </summary>
            public Money ToNearestRounding { get; init; }
        }
    }
}