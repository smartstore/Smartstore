using System;
using System.Linq;

namespace Smartstore.Core.Checkout.GiftCards
{
    public static class GiftCardQueryExtensions
    {
        /// <summary>
        /// Applies date time filter to gift cards query
        /// </summary>
        public static IQueryable<GiftCard> ApplyTimeFilter(this IQueryable<GiftCard> query, DateTime? startTime = null, DateTime? endTime = null)
        {
            Guard.NotNull(query, nameof(query));

            if (startTime.HasValue)
                query = query.Where(x => x.CreatedOnUtc >= startTime);

            if (endTime.HasValue)
                query = query.Where(x => x.CreatedOnUtc <= endTime);

            return query;
        }

        /// <summary>
        /// Applies coupon code filter to gift cards query
        /// </summary>
        public static IQueryable<GiftCard> ApplyCouponFilter(this IQueryable<GiftCard> query, string[] couponCodes)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(couponCodes, nameof(couponCodes));

            return query.Where(x => couponCodes.Contains(x.CouponCode));
        }
    }
}
