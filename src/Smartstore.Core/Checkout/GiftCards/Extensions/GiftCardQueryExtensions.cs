using System;
using System.Linq;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    /// <summary>
    /// Gift card query extensions
    /// </summary>
    public static class GiftCardQueryExtensions
    {
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

        /// <summary>
        /// Applies standard filter to gift cards query.
        /// Store filter is applied based on <see cref="GiftCard.PurchasedWithOrderItem"/> store identifier.
        /// May exclude not activated gift cards.
        /// </summary>        
        /// <remarks>
        /// Accesses <see cref="GiftCard.PurchasedWithOrderItem"/> and <see cref="OrderItem.Order"/>. 
        /// The caller is responsible for eager loading.
        /// </remarks>
        /// <returns>
        /// Valid gift cards for store with <see cref="storeId"/> ordered descending by <see cref="GiftCard.CreatedOnUtc"/>.
        /// </returns>
        public static IOrderedQueryable<GiftCard> ApplyStandardFilter(this IQueryable<GiftCard> query, int storeId = 0, bool includeInactive = false)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeInactive)
            {
                query = query.Where(x => x.IsGiftCardActivated);
            }

            query = query.Where(x => storeId == 0
                || x.PurchasedWithOrderItem.Order.StoreId == 0
                || x.PurchasedWithOrderItem.Order.StoreId == storeId);

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies order filter to gift cards query.
        /// </summary>
        /// <returns>
        /// Gift cards filtered by <see cref="orderIds"/> and ordered by identifiers.
        /// </returns>
        public static IOrderedQueryable<GiftCard> ApplyOrderFilter(this IQueryable<GiftCard> query, int[] orderIds)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(orderIds, nameof(orderIds));

            return query
                .Where(x => x.PurchasedWithOrderItemId.HasValue && orderIds.Contains(x.PurchasedWithOrderItemId.Value))
                .OrderBy(x => x.Id);
        }
    }
}
