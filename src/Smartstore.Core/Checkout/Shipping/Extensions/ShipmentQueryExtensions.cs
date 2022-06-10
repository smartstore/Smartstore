namespace Smartstore.Core.Checkout.Shipping
{
    /// <summary>
    /// Shipment query extensions
    /// </summary>
    public static partial class ShipmentQueryExtensions
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

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies order filter to shipment query ordered by <see cref="Shipment.OrderId"/> then by <see cref="Shipment.CreatedOnUtc"/>
        /// </summary>
        public static IOrderedQueryable<Shipment> ApplyOrderFilter(this IQueryable<Shipment> query, int[] orderIds)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(orderIds, nameof(orderIds));

            return query
                .Where(x => orderIds.Contains(x.OrderId))
                .OrderBy(x => x.OrderId)
                .ThenBy(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies shipment filter to query ordered by shipment identifier then by <see cref="Shipment.CreatedOnUtc"/>
        /// </summary>
        public static IOrderedQueryable<Shipment> ApplyShipmentFilter(this IQueryable<Shipment> query, int[] shipmentIds)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(shipmentIds, nameof(shipmentIds));

            return query
                .Where(x => shipmentIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .ThenBy(x => x.CreatedOnUtc);
        }
    }
}