using System.Collections.Generic;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Represents calculated shopping cart totals
    /// </summary>
    public partial class ShoppingCartTotal
    {
        public ShoppingCartTotal(decimal? totalAmount)
        {
            TotalAmount = totalAmount;
        }

        public static implicit operator decimal?(ShoppingCartTotal obj)
            => obj.TotalAmount;

        public static implicit operator ShoppingCartTotal(decimal? obj) 
            => new(obj);

        /// <summary>
        /// Total amount of the shopping cart. <c>null</c> if the cart total couldn't be calculated now
        /// </summary>
        public decimal? TotalAmount { get; init; }

        /// <summary>
        /// Rounding amount
        /// </summary>
        public decimal RoundingAmount { get; set; }

        /// <summary>
        /// Applied discount amount
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Reward points to redeem
        /// </summary>
        public int RedeemedRewardPoints { get; set; }

        /// <summary>
        /// Reward points amount to redeem (in primary store currency)
        /// </summary>
        public decimal RedeemedRewardPointsAmount { get; set; }

        /// <summary>
        /// Credit balance
        /// </summary>
        public decimal CreditBalance { get; set; }

        /// <summary>
        /// Applied discount
        /// </summary>
        public Discount AppliedDiscount { get; set; }

        /// <summary>
        /// Applied gift cards
        /// </summary>
        public List<AppliedGiftCard> AppliedGiftCards { get; set; }

        /// <summary>
        /// Shopping cart total and rounding amount. Converted from primary store currency
        /// </summary>
        public ConvertedAmounts ConvertedAmount { get; set; } = new();
        
        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Returns formatted <see cref="TotalAmount"/>
        /// </summary>
        public override string ToString() 
            => (TotalAmount ?? decimal.Zero).FormatInvariant();

        /// <summary>
        /// Represents converted amount of <see cref="ShoppingCartTotal.TotalAmount"/> and <see cref="ShoppingCartTotal.RoundingAmount"/>
        /// </summary>
        public class ConvertedAmounts
        {
            /// <summary>
            /// Converted total amount of the shopping cart.
            /// </summary>
            /// <remarks>
            /// <c>null</c> if the cart total couldn't be calculated now
            /// </remarks>
            public decimal? TotalAmount { get; set; }

            /// <summary>
            /// Converted rounding amount
            /// </summary>
            public decimal RoundingAmount { get; set; }
        }
    }
}