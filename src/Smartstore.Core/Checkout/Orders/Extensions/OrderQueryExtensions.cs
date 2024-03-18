using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore
{
    public static class OrderQueryExtensions
    {
        /// <summary>
        /// Includes the billing address graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<Order, Country> IncludeBillingAddress(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query
                .Include(x => x.BillingAddress.StateProvince)
                .Include(x => x.BillingAddress.Country);
        }

        /// <summary>
        /// Includes the shipping address graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<Order, Country> IncludeShippingAddress(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query
                .Include(x => x.ShippingAddress.StateProvince)
                .Include(x => x.ShippingAddress.Country);
        }

        /// <summary>
        /// Includes the gift card history graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<Order, GiftCard> IncludeGiftCardHistory(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query
                .AsSplitQuery()
                .Include(x => x.GiftCardUsageHistory)
                .ThenInclude(x => x.GiftCard);
        }

        /// <summary>
        /// Includes the customer graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<Order, CustomerRole> IncludeCustomer(this IQueryable<Order> query,
            bool includeRewardPoints = false)
        {
            Guard.NotNull(query);

            if (includeRewardPoints)
            {
                query = query
                    .Include(x => x.RedeemedRewardPointsEntry)
                    .Include(x => x.Customer.RewardPointsHistory);
            }

            return query
                .AsSplitQuery()
                .Include(x => x.Customer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole);
        }

        /// <summary>
        /// Includes the order item graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<Order, Product> IncludeOrderItems(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query
                .AsSplitQuery()
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product);
        }

        /// <summary>
        /// Includes the shipments graph for eager loading.
        /// </summary>
        public static IIncludableQueryable<Order, ICollection<ShipmentItem>> IncludeShipments(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query
                .AsSplitQuery()
                .Include(x => x.ShippingAddress)
                .Include(x => x.Shipments)
                .ThenInclude(x => x.ShipmentItems);
        }


        /// <summary>
        /// Applies a standard filter and sorts by <see cref="Order.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Ordered order query.</returns>
        public static IOrderedQueryable<Order> ApplyStandardFilter(this IQueryable<Order> query, int? customerId = null, int? storeId = null)
        {
            Guard.NotNull(query);

            if (customerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            return query.OrderByDescending(o => o.CreatedOnUtc);
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
            Guard.NotNull(query);

            if (!orderStatusIds.IsNullOrEmpty())
            {
                query = query.Where(x => orderStatusIds.Contains(x.OrderStatusId));
            }

            if (!paymentStatusIds.IsNullOrEmpty())
            {
                query = query.Where(x => paymentStatusIds.Contains(x.PaymentStatusId));
            }

            if (!shippingStatusIds.IsNullOrEmpty())
            {
                query = query.Where(x => shippingStatusIds.Contains(x.ShippingStatusId));
            }

            return query;
        }

        /// <summary>
        /// Applies a filter for payment methods and transaction\capture identifier.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="paymentMethodSystemNames">System name of payment methods.</param>
        /// <param name="authorizationTransactionId">Authorization transaction identifier. Typically a foreign identifier provided by the payment provider.</param>
        /// <param name="captureTransactionId">Capture transaction identifier. Typically a foreign identifier provided by the payment provider.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyPaymentFilter(
            this IQueryable<Order> query,
            string[] paymentMethodSystemNames = null,
            string authorizationTransactionId = null,
            string captureTransactionId = null)
        {
            Guard.NotNull(query);

            if (authorizationTransactionId.HasValue())
            {
                query = query.Where(x => x.AuthorizationTransactionId == authorizationTransactionId);
            }

            if (captureTransactionId.HasValue())
            {
                query = query.Where(x => x.CaptureTransactionId == captureTransactionId);
            }

            if (!paymentMethodSystemNames.IsNullOrEmpty())
            {
                query = query.Where(x => paymentMethodSystemNames.Contains(x.PaymentMethodSystemName));
            }

            return query;
        }

        /// <summary>
        /// Applies a filter for billing data.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="billingEmail">Email of billing address.</param>
        /// <param name="billingName">First or last name of billing address.</param>
        /// <param name="billingCountryIds">Billing country identifiers.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyBillingFilter(this IQueryable<Order> query,
            string billingEmail = null,
            string billingName = null,
            int[] billingCountryIds = null)
        {
            Guard.NotNull(query);

            if (billingEmail.HasValue())
            {
                query = query.Where(x => x.BillingAddress != null 
                    && !string.IsNullOrEmpty(x.BillingAddress.Email) && x.BillingAddress.Email.Contains(billingEmail));
            }

            if (billingName.HasValue())
            {
                query = query.Where(x => x.BillingAddress != null
                    && (!string.IsNullOrEmpty(x.BillingAddress.LastName) && x.BillingAddress.LastName.Contains(billingName)
                    || !string.IsNullOrEmpty(x.BillingAddress.FirstName) && x.BillingAddress.FirstName.Contains(billingName)
                ));
            }

            if (!billingCountryIds.IsNullOrEmpty())
            {
                query = query.Where(x => x.BillingAddress != null && billingCountryIds.Contains((int)x.BillingAddress.CountryId));
            }

            return query;
        }

        /// <summary>
        /// Applies a filter for incomplete orders. 
        /// Filters query for !<see cref="OrderStatus.Cancelled"/> and <see cref="ShippingStatus.NotYetShipped"/> or <see cref="PaymentStatus.Pending"/>.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <returns>Query without completed orders.</returns>
        public static IQueryable<Order> ApplyIncompleteOrdersFilter(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query
                .Where(x => x.OrderStatusId != (int)OrderStatus.Cancelled
                && (x.ShippingStatusId == (int)ShippingStatus.NotYetShipped || x.PaymentStatusId == (int)PaymentStatus.Pending));
        }

        /// <summary>
        /// Applies a never sold products filter to query.
        /// </summary>
        /// <param name="query">Orders query with date filter already applied.</param>
        /// <param name="includeHidden">A value indicating whether to include unpublished products.</param>
        /// <returns>Query with products which have never been sold.</returns>
        public static IQueryable<Product> ApplyNeverSoldProductsFilter(this IQueryable<Order> query, bool includeHidden = false)
        {
            Guard.NotNull(query);

            var groupedProductId = (int)ProductType.GroupedProduct;

            var db = query.GetDbContext<SmartDbContext>();

            var orderItemProductIdsQuery = db.OrderItems
                .AsNoTracking()
                .Join(query.AsNoTracking(), orderItem => orderItem.OrderId, order => order.Id, (orderItem, order) => orderItem)
                .Select(x => x.ProductId)
                .Distinct();

            return db.Products
                .AsNoTracking()
                .ApplyStandardFilter(includeHidden)
                .Where(x => !orderItemProductIdsQuery.Contains(x.Id) && x.ProductTypeId != groupedProductId)
                .OrderBy(x => x.Name);
        }

        /// <summary>
        /// Selects <see cref="OrderDataPoint"/> from query.
        /// </summary>
        /// <param name="query">Order query from which to select.</param>
        /// <returns><see cref="IQueryable"/> of <see cref="OrderDataPoint"/>.</returns>
        public static IQueryable<OrderDataPoint> SelectAsOrderDataPoint(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query.Select(x => new OrderDataPoint
            {
                CreatedOn = x.CreatedOnUtc,
                OrderTotal = x.OrderTotal,
                OrderStatusId = x.OrderStatusId,
                PaymentStatusId = x.PaymentStatusId,
                ShippingStatusId = x.ShippingStatusId
            });
        }

        /// <summary>
        /// Groups orders by <see cref="Order.CustomerId"/> and selects <see cref="TopCustomerReportLine"/> from <paramref name="query"/>.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="sorting"><see cref="ReportSorting"/> to apply.</param>
        /// <returns><see cref="TopCustomerReportLine"/> query.</returns>
        public static IQueryable<TopCustomerReportLine> SelectAsTopCustomerReportLine(this IQueryable<Order> query,
            ReportSorting sorting = ReportSorting.ByQuantityDesc)
        {
            Guard.NotNull(query);

            var groupedQuery =
                from o in query
                group o by o.CustomerId into grp
                select new TopCustomerReportLine
                {
                    CustomerId = grp.Key,
                    OrderTotal = grp.Sum(x => x.OrderTotal),
                    OrderCount = grp.Count()
                };

            groupedQuery = sorting switch
            {
                ReportSorting.ByAmountAsc => groupedQuery.OrderBy(x => x.OrderTotal),
                ReportSorting.ByAmountDesc => groupedQuery.OrderByDescending(x => x.OrderTotal),
                ReportSorting.ByQuantityAsc => groupedQuery.OrderBy(x => x.OrderCount).ThenByDescending(x => x.OrderTotal),
                _ => groupedQuery.OrderByDescending(x => x.OrderCount).ThenByDescending(x => x.OrderTotal),
            };

            return groupedQuery;
        }

        /// <summary>
        /// Gets orders product costs async.
        /// </summary>
        /// <param name="query">Orders query.</param>
        /// <returns>Product costs</returns>
        public static Task<decimal> GetOrdersProductCostsAsync(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            var db = query.GetDbContext<SmartDbContext>();

            return db.OrderItems
                .Join(query, orderItem => orderItem.OrderId, order => order.Id, (orderItem, order) => orderItem)
                .SumAsync(x => ((decimal?)x.ProductCost ?? decimal.Zero) * x.Quantity);
        }

        /// <summary>
        /// Gets orders total async.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <returns>Orders totals.</returns>
        public static Task<decimal> GetOrdersTotalAsync(this IQueryable<Order> query)
        {
            Guard.NotNull(query);

            return query.SumAsync(x => (decimal?)x.OrderTotal ?? decimal.Zero);
        }
    }
}