using System.Collections.Generic;

namespace Smartstore.Core.Catalog.Discounts
{
    public static partial class DiscountExtensions
    {
        /// <summary>
        /// Gets the discount amount for the specified value.
        /// </summary>
        /// <param name="discount">Discount.</param>
        /// <param name="amount">Amount</param>
        /// <returns>The discount amount.</returns>
        public static decimal GetDiscountAmount(this Discount discount, decimal amount)
        {
            Guard.NotNull(discount, nameof(discount));

            if (discount.UsePercentage)
            {
                return (decimal)((((float)amount) * ((float)discount.DiscountPercentage)) / 100f);
            }
            else
            {
                return discount.DiscountAmount;
            }
        }

        /// <summary>
        /// Gets the discount that achieves the highest discount amount other than zero.
        /// </summary>
        /// <param name="discounts">List of discounts.</param>
        /// <param name="amount">Amount without discount (for percentage discounts).</param>
        /// <returns>Discount that achieves the highest discount amount other than zero.</returns>
        public static Discount GetPreferredDiscount(this ICollection<Discount> discounts, decimal amount)
        {
            Guard.NotNull(discounts, nameof(discounts));

            Discount preferredDiscount = null;
            decimal? maximumDiscountValue = null;

            foreach (var discount in discounts)
            {
                var currentDiscountValue = discount.GetDiscountAmount(amount);
                if (currentDiscountValue != decimal.Zero)
                {
                    if (!maximumDiscountValue.HasValue || currentDiscountValue > maximumDiscountValue)
                    {
                        maximumDiscountValue = currentDiscountValue;
                        preferredDiscount = discount;
                    }
                }
            }

            return preferredDiscount;
        }
    }
}
