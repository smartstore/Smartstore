using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
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
            Guard.NotNull(query);

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

        /// <summary>
        /// Filters order items by criteria met by assigned orders.
        /// </summary>
        /// <param name="storeId"><see cref="Order.StoreId"/></param>
        /// <param name="fromUtc">Earliest <see cref="Order.CreatedOnUtc"/></param>
        /// <param name="toUtc">Latest <see cref="Order.CreatedOnUtc"/></param>
        /// <param name="orderStatusIds">IN <see cref="Order.OrderStatusId"/></param>
        /// <param name="paymentStatusIds">IN IN <see cref="Order.PaymentStatusId"/></param>
        /// <param name="shippingStatusIds">IN <see cref="Order.ShippingStatusId"/></param>
        /// <param name="billingCountryId">Order.BillingAddress.CountryId</param>
        public static IQueryable<OrderItem> ApplyOrderFilter(this IQueryable<OrderItem> query,
            int storeId = 0,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int[] orderStatusIds = null,
            int[] paymentStatusIds = null,
            int[] shippingStatusIds = null,
            int? billingCountryId = null)
        {
            Guard.NotNull(query);

            var db = query.GetDbContext<SmartDbContext>();

            query =
                from oi in query
                join o in db.Orders on oi.OrderId equals o.Id
                where (storeId == 0 || o.StoreId == storeId) &&
                    (!fromUtc.HasValue || o.CreatedOnUtc >= fromUtc.Value) &&
                    (!toUtc.HasValue || o.CreatedOnUtc <= toUtc.Value) &&
                    (orderStatusIds == null || orderStatusIds.Contains(o.OrderStatusId)) &&
                    (paymentStatusIds == null || paymentStatusIds.Contains(o.PaymentStatusId)) &&
                    (shippingStatusIds == null || shippingStatusIds.Contains(o.ShippingStatusId)) &&
                    (!billingCountryId.HasValue || (o.BillingAddress != null && o.BillingAddress.CountryId == billingCountryId))
                select oi;

            return query;
        }

        /// <summary>
        /// Filters order items by criteria met by assigned line items.
        /// </summary>
        /// <param name="productIds">Filters all <see cref="OrderItem"/>s that contain any <paramref name="productIds"/>.</param>
        /// <param name="includeHidden">A value indicating Include unpublished products.</param>
        public static IQueryable<OrderItem> ApplyProductFilter(this IQueryable<OrderItem> query,
            int[] productIds = null,
            bool includeHidden = false)
        {
            // TODO: (mh) (core) Add more params to OrderItemQueryExtensions.ApplyProductFilter()
            Guard.NotNull(query);

            var db = query.GetDbContext<SmartDbContext>();

            query =
                from oi in query
                join p in db.Products on oi.ProductId equals p.Id
                where (productIds == null || productIds.Contains(p.Id))
                    && (includeHidden || p.Published)
                    && !p.IsSystemProduct
                select oi;

            return query;
        }

        /// <summary>
        /// Applies a selection for also purchased product ids.
        /// </summary>
        /// <param name="query">Query of <see cref="OrderItem"/>s to select from.</param>
        /// <param name="productId">Product identifier to also receive products purchased with the product.</param>
        /// <param name="recordsToReturn">Number of products to return. <c>Null</c> or 0 to retrieve all.</param>
        /// <param name="storeId">Store identifier to get orders from.</param>
        /// <param name="includeHidden">A value indicating whether to include unpublished products.</param>
        /// <returns>Query of product identifiers.</returns>
        public static IQueryable<int> SelectAlsoPurchasedProductIds(this IQueryable<OrderItem> query,
            int productId, 
            int? recordsToReturn = 5, 
            int storeId = 0,
            bool includeHidden = false)
        {
            Guard.NotNull(query);

            if (productId == 0)
            {
                return Array.Empty<int>().AsQueryable();
            }

            var db = query.GetDbContext<SmartDbContext>();

            var orderIdsQuery = db.OrderItems
                .Where(x => x.ProductId == productId)
                .Select(x => x.OrderId);

            var orderItemsQuery = 
                from orderItem in db.OrderItems
                join p in db.Products on orderItem.ProductId equals p.Id
                where orderIdsQuery.Contains(orderItem.OrderId) &&
                (p.Id != productId) &&
                (includeHidden || p.Published) &&
                (!orderItem.Order.Deleted) &&
                (storeId == 0 || orderItem.Order.StoreId == storeId) &&
                (!p.Deleted) && (!p.IsSystemProduct) &&
                (includeHidden || p.Published)
                select new { orderItem, p };

            var productIdsQuery1 = 
                from oi in orderItemsQuery
                group oi by oi.p.Id into g
                select new
                {
                    ProductId = g.Key,
                    ProductsPurchased = g.Sum(x => x.orderItem.Quantity),
                };

            var productIdsQuery2 = productIdsQuery1
                .OrderByDescending(x => x.ProductsPurchased)
                .Select(x => x.ProductId);

            if (recordsToReturn > 0)
            {
                productIdsQuery2 = productIdsQuery2.Take(recordsToReturn.Value);
            }

            return productIdsQuery2;
        }

        /// <summary>
        /// Applies a selection for bestsellers report.
        /// </summary>
        /// <param name="query">Order item query to select report from.</param>
        /// <param name="sorting">Sorting setting to define Bestsellers report type.</param>
        /// <returns>Query of bestsellers report.</returns>
        public static IQueryable<BestsellersReportLine> SelectAsBestsellersReportLine(this IQueryable<OrderItem> query, ReportSorting sorting = ReportSorting.ByQuantityDesc)
        {
            Guard.NotNull(query);

            var selector = query
                .GroupBy(x => x.ProductId)
                .Select(x => new BestsellersReportLine
                {
                    ProductId = x.Key,
                    TotalAmount = x.Sum(x => x.PriceExclTax),
                    TotalQuantity = x.Sum(x => x.Quantity)
                });

            selector = sorting switch
            {
                ReportSorting.ByAmountAsc => selector.OrderBy(x => x.TotalAmount),
                ReportSorting.ByAmountDesc => selector.OrderByDescending(x => x.TotalAmount),
                ReportSorting.ByQuantityAsc => selector.OrderBy(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
                _ => selector.OrderByDescending(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
            };

            return selector;
        }
    }
}
