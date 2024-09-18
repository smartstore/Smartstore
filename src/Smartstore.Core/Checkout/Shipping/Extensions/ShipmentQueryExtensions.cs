using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;

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

        /// <summary>
        /// Selects shipments that the currently authenticated customer is authorized to access.
        /// </summary>
        /// <param name="query">Shipment query to filter from.</param>
        /// <param name="authorizedStoreIds">Ids of stores customer has access to</param>
        /// <returns><see cref="IQueryable"/> of <see cref="Shipment"/>.</returns>
        public static IQueryable<Shipment> ApplyCustomerStoreFilter(this IQueryable<Shipment> query, int[] authorizedStoreIds)
        {
            Guard.NotNull(query);
            if (!authorizedStoreIds.IsNullOrEmpty())
            {
                query = query.Where(s => authorizedStoreIds.Contains(s.Order.StoreId));
            }
            return query;
        }
    }
}