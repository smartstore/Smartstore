using System;
using System.Linq;

namespace Smartstore.Core.Checkout.GiftCards
{
    /// <summary>
    /// Gift card query extensions
    /// </summary>
    public static class GiftCardQueryExtension
    {
        /// <summary>
        /// Get gift cards within time period
        /// </summary>        
        /// <param name="startTime">Start date time inclusive</param>
        /// <param name="endTime">End date time inclusive</param>
        /// <returns>Gift cards query filtered by date time</returns>
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
        /// Gets gift cards by coupon codes
        /// </summary>        
        /// <param name="couponCodes">Coupon codes array to match</param>
        /// <returns>Gift cards query filtered by coupon codes</returns>
        public static IQueryable<GiftCard> ApplyCouponCodeFilter(this IQueryable<GiftCard> query, string[] couponCodes)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => couponCodes.Contains(x.CouponCode));
        }
    }
}
