using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore
{
    /// <summary>
    /// Gift card query extensions
    /// </summary>
    public static class GiftCardQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts by <see cref="GiftCard.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Gift cards query.</param>
        /// <param name="includeInactive">Filter by <see cref="GiftCard.IsGiftCardActivated"/>.</param>
        /// <returns>Gift cards query.</returns>
        public static IOrderedQueryable<GiftCard> ApplyStandardFilter(this IQueryable<GiftCard> query, bool includeInactive = false)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeInactive)
            {
                query = query.Where(x => x.IsGiftCardActivated);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies an order filter and sorts by <see cref="GiftCard.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Gift cards query.</param>
        /// <param name="orderIds">Order identifiers to filter.</param>
        /// <returns>Gift cards query.</returns>
        public static IOrderedQueryable<GiftCard> ApplyOrderFilter(this IQueryable<GiftCard> query, int[] orderIds)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(orderIds, nameof(orderIds));

            query = query.Where(x => x.PurchasedWithOrderItem != null && orderIds.Contains(x.PurchasedWithOrderItem.OrderId));

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
        public static IQueryable<GiftCard> ApplyCouponFilter(this IQueryable<GiftCard> query, params string[] couponCodes)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(couponCodes, nameof(couponCodes));

            return query.Where(x => couponCodes.Contains(x.GiftCardCouponCode));
        }
    }
}
