using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Smartstore.Core.Checkout.Shipping
{
    public static class ShipmentQueryExtensions
    {
        /// <summary>
        /// Applies date time filter to shipment query ordered by <see cref="Shipment.CreatedOnUtc"/>
        /// </summary>
        public static IOrderedQueryable<Shipment> ApplyTimeFilter(this IQueryable<Shipment> query, DateTime? startTime = null, DateTime? endTime = null)
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

            return query.OrderBy(x => x.CreatedOnUtc);
        }
    }
}
