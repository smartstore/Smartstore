using System;
using System.Linq;

namespace Smartstore.Core.Checkout.GiftCards
{
    public static class GiftCardQueryExtension
    {
        /// <summary>
        /// Applies time filter to gift cards.
        /// </summary>        
        /// <param name="startTime">Start date time inclusive</param>
        /// <param name="endTime">End date time inclusive</param>
        public static IQueryable<GiftCard> ApplyDateTimeFilter(this IQueryable<GiftCard> query, DateTime? startTime = null, DateTime? endTime = null)
        {
            Guard.NotNull(query, nameof(query));

            if (startTime.HasValue)
                query = query.Where(x => x.CreatedOnUtc >= startTime);

            if (startTime.HasValue)
                query = query.Where(x => x.CreatedOnUtc <= endTime);

            return query;
        }

        /// <summary>
        /// Applies coupon codes filter
        /// </summary>        
        /// <param name="couponCodes">Coupon codes array to match</param>
        public static IQueryable<GiftCard> ApplyCouponCodeFilter(this IQueryable<GiftCard> query, string[] couponCodes)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => couponCodes.Contains(x.CouponCode));
        }
    }
}
