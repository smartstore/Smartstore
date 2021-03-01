using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;

namespace Smartstore
{
    public static partial class OrderItemQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter for order or customer identifier.
        /// </summary>
        /// <param name="query">Order item query.</param>
        /// <param name="orderId">Order identifier.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <returns>Order item query.</returns>
        public static IQueryable<OrderItem> ApplyStandardFilter(this IQueryable<OrderItem> query, int? orderId = null, int? customerId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (orderId.HasValue)
            {
                query = query.Where(x => x.OrderId == orderId);
            }

            if (customerId.HasValue)
            {
                var db = query.GetDbContext<SmartDbContext>();

                query =
                    from oi in query
                    join o in db.Orders.AsNoTracking() on oi.OrderId equals o.Id
                    where o.CustomerId == customerId.Value
                    select oi;
            }

            return query;
        }
    }
}
