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
        /// Gets gift card by identifier
        /// </summary>
        /// <param name="giftCardId">Gift card identifier</param>
        /// <returns>Gift card query filtered by card identifier</returns>
        public static IQueryable<GiftCard> ApplyCardFilter(this IQueryable<GiftCard> query, int giftCardId)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => x.Id == giftCardId);
        }

        /// <summary>
        /// Gets gift cards by activation
        /// </summary>
        /// <param name="isActivated">Whether gift cards are activated or not</param>
        /// <returns>Gift cards query filtered by activation</returns>
        public static IQueryable<GiftCard> ApplyActivationFilter(this IQueryable<GiftCard> query, bool isActivated)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => x.IsActivated == isActivated);
        }

        /// <summary>
        /// Gets gift cards by orderItemId
        /// </summary>
        /// <param name="orderItemId">Purchased with order item identifier</param>
        /// <returns>Gift cards query filtered by order item identifier</returns>
        public static IQueryable<GiftCard> ApplyOrderItemFilter(this IQueryable<GiftCard> query, int orderItemId)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => x.OrderItemId == orderItemId);
        }

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

        /// <summary>
        /// Gets gift cards by coupon code
        /// </summary>        
        /// <param name="couponCodes">Coupon code to match</param>
        /// <returns>Gift cards query filtered by coupon code</returns>
        public static IQueryable<GiftCard> ApplyCouponCodeFilter(this IQueryable<GiftCard> query, string couponCode)
        {
            Guard.NotNull(query, nameof(query));

            return query.Where(x => x.CouponCode == couponCode);
        }
    }
}
