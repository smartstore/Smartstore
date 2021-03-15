using System;
using System.Linq;
using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore
{
    /// <summary>
    /// Gift card query extensions
    /// </summary>
    public static class GiftCardQueryExtensions
    {
        /// <summary>
        /// Applies standard filter to gift cards query and sorts by <see cref="GiftCard.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="isActivated">Filter by <see cref="GiftCard.IsGiftCardActivated"/>.</param>
        /// <remarks>Accesses <see cref="GiftCard.PurchasedWithOrderItem"/>. The caller is responsible for eager loading.</remarks>
        /// <returns>Gift cards query.</returns>
        public static IOrderedQueryable<GiftCard> ApplyStandardFilter(this IQueryable<GiftCard> query, int? orderId = null, bool? isActivated = null)
        {
            Guard.NotNull(query, nameof(query));

            if (orderId.HasValue)
            {
                query = query.Where(x => x.PurchasedWithOrderItem != null && x.PurchasedWithOrderItem.OrderId == orderId.Value);
            }

            if (isActivated.HasValue)
            {
                query = query.Where(x => x.IsGiftCardActivated == isActivated.Value);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies date time filter to gift cards query.
        /// </summary>
        public static IQueryable<GiftCard> ApplyTimeFilter(this IQueryable<GiftCard> query, DateTime? startTime = null, DateTime? endTime = null)
        {
            Guard.NotNull(query, nameof(query));

            if (startTime.HasValue)
            {
                query = query.Where(x => x.CreatedOnUtc >= startTime);
            }

            if (endTime.HasValue)
            {
                query = query.Where(x => x.CreatedOnUtc <= endTime);
            }

            return query;
        }

        /// <summary>
        /// Applies coupon code filter to gift cards query
        /// </summary>
        public static IQueryable<GiftCard> ApplyCouponFilter(this IQueryable<GiftCard> query, string[] couponCodes)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(couponCodes, nameof(couponCodes));

            return query.Where(x => couponCodes.Contains(x.GiftCardCouponCode));
        }
    }
}
