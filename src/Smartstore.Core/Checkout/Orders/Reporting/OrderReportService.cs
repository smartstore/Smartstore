using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Reporting
{
    // TODO: (ms) (core) Change decimal to money
    public partial class OrderReportService /*: IOrderReportService*/
    {
        private readonly SmartDbContext _db;

        public OrderReportService(SmartDbContext db)
        {
            _db = db;
        }

        // TODO: (ms) (core) This method seems to be not needed
        //public virtual async Task<OrderAverageReportLine> GetOrderAverageReportLineAsync(
        //    int storeId,
        //    int[] orderStatusIds,
        //    int[] paymentStatusIds,
        //    int[] shippingStatusIds,
        //    DateTime? startTimeUtc,
        //    DateTime? endTimeUtc,
        //    string billingEmail,
        //    bool ignoreCancelledOrders = false)
        //{
        //    //var query = _db.Orders
        //    //    .ApplyStandardFilter(storeId: storeId)
        //    //    .ApplyDateFilter(startTimeUtc, endTimeUtc)
        //    //    .ApplyBillingFilter(billingEmail)
        //    //    .ApplyStatusFilter(orderStatusIds, paymentStatusIds, shippingStatusIds);

        //}

        // TODO: (ms) (core) refactor method parameters -> qury extensions - reutrns query 
        // apply status filter
        // apply shipping filer
        // apply time filter
        public virtual Task<IPagedList<BestSellersReportLine>> BestSellersReportAsync(
            int storeId,
            DateTime? startTime,
            DateTime? endTime,
            int? orderStatusId = null,
            int? paymentStatusId = null,
            int? shippingStatusId = null,
            int? billingCountryId = null,
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            ReportSorting sorting = ReportSorting.ByQuantityDesc,
            bool showHidden = false)
        {
            var query =
                from orderItem in _db.OrderItems
                join o in _db.Orders.AsNoTracking() on orderItem.OrderId equals o.Id
                join p in _db.Products.AsNoTracking() on orderItem.ProductId equals p.Id
                where (storeId == 0 || storeId == o.StoreId)
                    && (!startTime.HasValue || startTime.Value <= o.CreatedOnUtc)
                    && (!endTime.HasValue || endTime.Value >= o.CreatedOnUtc)
                    && (!orderStatusId.HasValue || orderStatusId == o.OrderStatusId)
                    && (!paymentStatusId.HasValue || paymentStatusId == o.PaymentStatusId)
                    && (!shippingStatusId.HasValue || shippingStatusId == o.ShippingStatusId)
                    && (billingCountryId == 0 || o.BillingAddress.CountryId == billingCountryId)
                    && (!p.IsSystemProduct)
                    && (showHidden || p.Published)
                select orderItem;

            // Group by product ID.
            var groupedQuery =
                from orderItem in query
                group orderItem by orderItem.ProductId into g
                select new BestSellersReportLine
                {
                    ProductId = g.Key,
                    TotalAmount = g.Sum(x => x.PriceExclTax),
                    TotalQuantity = g.Sum(x => x.Quantity)
                };

            groupedQuery = sorting switch
            {
                ReportSorting.ByAmountAsc => groupedQuery.OrderBy(x => x.TotalAmount),
                ReportSorting.ByAmountDesc => groupedQuery.OrderByDescending(x => x.TotalAmount),
                ReportSorting.ByQuantityAsc => groupedQuery.OrderBy(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
                _ => groupedQuery.OrderByDescending(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
            };

            return groupedQuery.ToPagedList(pageIndex, pageSize).LoadAsync();
        }

        // TODO: (ms) (core) This method seems to be not needed
        //public virtual Task<int> GetPurchaseCountAsync(int productId)
        //{
        //    if (productId == 0)
        //        return Task.FromResult(0);

        //    var query =
        //        from orderItem in _db.OrderItems
        //        where orderItem.ProductId == productId
        //        group orderItem by orderItem.Id into g
        //        select new { ProductsPurchased = g.Sum(x => x.Quantity) };

        //    return query.Select(x => x.ProductsPurchased).FirstOrDefaultAsync();
        //}

        /// <summary>
        /// So this retrieves product ids, which have also been purchased with productId
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="recordsToReturn"></param>
        /// <param name="storeId"></param>
        /// <param name="showHidden"></param>
        /// <returns></returns>
        public virtual async Task<int[]> GetAlsoPurchasedProductsIdsAsync(int productId, int? recordsToReturn = 5, int storeId = 0, bool showHidden = false)
        {
            if (productId == 0)
                return Array.Empty<int>();

            // This inner query retrieves all orderItems which contain the productId.
            var orderItems = await _db.OrderItems
                .Where(x => x.ProductId == productId)
                .Select(x => x.OrderId)
                .ToListAsync();

            var query = _db.OrderItems
                .Include(x => x.Order)
                .Join(_db.Products, x => x.ProductId, y => y.Id, (x, y) => new { OrderItem = x, Product = y })
                .Where(x => orderItems.Contains(x.OrderItem.OrderId)
                    && x.Product.Id != productId
                    && (showHidden || x.Product.Published)
                    && (storeId == 0 || x.OrderItem.Order.StoreId == storeId)
                    && !x.Product.IsSystemProduct)
                .GroupBy(x => x.Product.Id)
                .Select(x => new
                {
                    ProductId = x.Key,
                    ProductsPurchased = x.Sum(x => x.OrderItem.Quantity)
                })
                .OrderByDescending(x => x.ProductsPurchased)
                .AsQueryable();

            if (recordsToReturn.GetValueOrDefault() > 0)
            {
                query = query.Take(recordsToReturn.Value);
            }

            return await query.Select(x => x.ProductId).ToArrayAsync();
        }

        // extensions
        //public virtual Task<IPagedList<Product>> ProductsNeverSoldAsync(DateTime? startTime, DateTime? endTime, int pageIndex, int pageSize, bool showHidden = false)
        //{
        //    var groupedProductId = (int)ProductType.GroupedProduct;

        //    var query1 = (from orderItem in _db.OrderItems.AsNoTracking()
        //                  join o in _db.Orders.AsNoTracking() on orderItem.OrderId equals o.Id
        //                  where (!startTime.HasValue || startTime.Value <= o.CreatedOnUtc)
        //                    && (!endTime.HasValue || endTime.Value >= o.CreatedOnUtc)
        //                  select orderItem.ProductId).Distinct();

        //    var query2 = from p in _db.Products.AsNoTracking()
        //                 where !query1.Contains(p.Id)
        //                    && !p.IsSystemProduct
        //                    && (showHidden || p.Published)
        //                    && p.ProductTypeId != groupedProductId
        //                 orderby p.Name
        //                 select p;

        //    return query2.ToPagedList(pageIndex, pageSize).LoadAsync();
        //}

        // extneision
        //public virtual async Task<decimal> GetProfitAsync(IQueryable<Order> orderQuery)
        //{
        //    var productCost = await _db.OrderItems
        //        .Join(_db.Orders, orderItem => orderItem.OrderId, o => o.Id, (orderItem, o) => orderItem)
        //        .SumAsync(x => ((decimal?)x.ProductCost ?? decimal.Zero) * x.Quantity) ;

        //    var summary = await GetOrderAverageReportLineAsync(orderQuery);
        //    var profit = summary.SumOrders - summary.SumTax - productCost;

        //    return profit;
        //}

        //// apply incomplete orders filter extension -> extensions
        //public virtual Task<IList<OrderDataPoint>> GetIncompleteOrdersAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc)
        //{
        //    var query = _db.Orders
        //        .ApplyStandardFilter(storeId: storeId)
        //        .ApplyDateFilter(startTimeUtc, endTimeUtc)
        //        .Where(x => x.OrderStatusId != (int)OrderStatus.Cancelled
        //            && (x.ShippingStatusId == (int)ShippingStatus.NotYetShipped
        //            || x.PaymentStatusId == (int)PaymentStatus.Pending))
        //        .Select(x => new OrderDataPoint
        //        {
        //            CreatedOn = x.CreatedOnUtc,
        //            OrderTotal = x.OrderTotal,
        //            OrderStatusId = x.OrderStatusId,
        //            PaymentStatusId = x.PaymentStatusId,
        //            ShippingStatusId = x.ShippingStatusId
        //        });

        //    return query;
        //}

        //// TODO: (ms) (core) Maybe capsule dashboard-reports in its own service? -> extensin
        //public virtual Task<IPagedList<OrderDataPoint>> GetOrdersDashboardDataAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc, int pageIndex, int pageSize)
        //{
        //    var query = _db.Orders
        //        .ApplyStandardFilter(storeId: storeId)
        //        .ApplyDateFilter(startTimeUtc, endTimeUtc)
        //        .Select(x => new OrderDataPoint
        //        {
        //            CreatedOn = x.CreatedOnUtc,
        //            OrderTotal = x.OrderTotal,
        //            OrderStatusId = x.OrderStatusId
        //        });

        //    return query.ToPagedList(pageIndex, pageSize).LoadAsync();
        //}

        //// TODO: (ms) (core) This method seems to be not needed
        //public virtual Task<decimal> GetOrdersTotalAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc)
        //{
        //    return _db.Orders
        //        .ApplyStandardFilter(storeId: storeId)
        //        .ApplyDateFilter(startTimeUtc, endTimeUtc)
        //        .SumAsync(x => (decimal?)x.OrderTotal ?? decimal.Zero);
        //}
    }
}