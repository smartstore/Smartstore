using System.Linq;

namespace Smartstore.Core.Checkout.Orders
{
    public static class OrderQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyStandardFilter(this IQueryable<Order> query, int? customerId = null, int? storeId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (customerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            return query;
        }

        /// <summary>
        /// Applies a status filter.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="orderStatusIds">Order status identifiers.</param>
        /// <param name="paymentStatusIds">Payment status identifiers.</param>
        /// <param name="shippingStatusIds">Shipping status identifiers.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyStatusFilter(this IQueryable<Order> query, int[] orderStatusIds = null, int[] paymentStatusIds = null, int[] shippingStatusIds = null)
        {
            Guard.NotNull(query, nameof(query));

            if (orderStatusIds?.Any() ?? false)
            {
                query = query.Where(x => orderStatusIds.Contains(x.OrderStatusId));
            }

            if (paymentStatusIds?.Any() ?? false)
            {
                query = query.Where(x => paymentStatusIds.Contains(x.PaymentStatusId));
            }

            if (shippingStatusIds?.Any() ?? false)
            {
                query = query.Where(x => shippingStatusIds.Contains(x.ShippingStatusId));
            }

            return query;
        }
    }
}
