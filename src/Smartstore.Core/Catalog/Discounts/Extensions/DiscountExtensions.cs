using System.Runtime.CompilerServices;

namespace Smartstore.Core.Catalog.Discounts
{
    public static partial class DiscountExtensions
    {
        /// <summary>
        /// Gets the discount amount for the specified value.
        /// </summary>
        /// <param name="discount">Discount.</param>
        /// <param name="value">Amount value.</param>
        /// <returns>The discount amount.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal GetDiscountAmount(this Discount discount, decimal value)
        {
            Guard.NotNull(discount, nameof(discount));

            return discount.UsePercentage
                ? value * discount.DiscountPercentage / 100m
                : discount.DiscountAmount;
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
