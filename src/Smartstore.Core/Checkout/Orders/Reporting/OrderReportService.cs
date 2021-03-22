using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Reporting
{
    public partial class OrderReportService : IOrderReportService
    {
        private readonly SmartDbContext _db;

        public OrderReportService(SmartDbContext db)
        {
            _db = db;
        }

        //public virtual Task<IPagedList<BestsellersReportLine>> BestsellersReportAsync(
        //    int storeId,
        //    DateTime? startTime,
        //    DateTime? endTime,
        //    int? orderStatusId = null,
        //    int? paymentStatusId = null,
        //    int? shippingStatusId = null,
        //    int? billingCountryId = null,
        //    int pageIndex = 0,
        //    int pageSize = int.MaxValue,
        //    ReportSorting sorting = ReportSorting.ByQuantityDesc,
        //    bool includeHidden = false)
        //{
        //    var query = _db.OrderItems
        //        .Join(_db.Orders.AsNoTracking(), orderItem => orderItem.OrderId, o => o.Id, (orderItem, o) => orderItem)
        //        .Join(_db.Products.AsNoTracking(), orderItem => orderItem.ProductId, p => p.Id, (orderItem, p) => orderItem)
        //        .Where(x => (storeId == 0 || storeId == x.Order.StoreId)
        //            && (!startTime.HasValue || startTime.Value <= x.Order.CreatedOnUtc)
        //            && (!endTime.HasValue || endTime.Value >= x.Order.CreatedOnUtc)
        //            && (!orderStatusId.HasValue || orderStatusId == x.Order.OrderStatusId)
        //            && (!paymentStatusId.HasValue || paymentStatusId == x.Order.PaymentStatusId)
        //            && (!shippingStatusId.HasValue || shippingStatusId == x.Order.ShippingStatusId)
        //            && (billingCountryId == 0 || x.Order.BillingAddress.CountryId == billingCountryId)
        //            && (!x.Product.IsSystemProduct)
        //            && (includeHidden || x.Product.Published))
        //        .GroupBy(x => x.ProductId)
        //        .Select(x => new BestsellersReportLine
        //        {
        //            ProductId = x.Key,
        //            TotalAmount = x.Sum(x => x.PriceExclTax),
        //            TotalQuantity = x.Sum(x => x.Quantity)
        //        });

        //    query = sorting switch
        //    {
        //        ReportSorting.ByAmountAsc => query.OrderBy(x => x.TotalAmount),
        //        ReportSorting.ByAmountDesc => query.OrderByDescending(x => x.TotalAmount),
        //        ReportSorting.ByQuantityAsc => query.OrderBy(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
        //        _ => query.OrderByDescending(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
        //    };

        //    return query.ToPagedList(pageIndex, pageSize).LoadAsync();
        //}

        //public virtual Task<int[]> GetAlsoPurchasedProductIdsAsync(int productId, int? recordsToReturn = 5, int storeId = 0, bool includeHidden = false)
        //{
        //    if (productId == 0)
        //        return Task.FromResult(Array.Empty<int>());

        //    // This inner query retrieves all orderItems which contain the productId.
        //    var orderItems = _db.OrderItems
        //        .ApplyProductFilter(new[] { productId }, includeHidden)
        //        .Select(x => x.OrderId);

        //    var query = _db.OrderItems
        //        .ApplyProductFilter(includeHidden: includeHidden)
        //        .Include(x => x.Order)
        //        .Join(_db.Products, x => x.ProductId, y => y.Id, (x, y) => new { OrderItem = x, Product = x.Product })
        //        .Where(x => orderItems.Contains(x.OrderItem.OrderId)
        //            && x.Product.Id != productId
        //            && (storeId == 0 || x.OrderItem.Order.StoreId == storeId))
        //        .GroupBy(x => x.Product.Id)
        //        .Select(x => new
        //        {
        //            ProductId = x.Key,
        //            ProductsPurchased = x.Sum(x => x.OrderItem.Quantity)
        //        })
        //        .OrderByDescending(x => x.ProductsPurchased)
        //        .AsQueryable();

        //    if (recordsToReturn.GetValueOrDefault() > 0)
        //    {
        //        query = query.Take(recordsToReturn.Value);
        //    }

        //    return query.Select(x => x.ProductId).ToArrayAsync();
        //}

        //public virtual Task<IPagedList<Product>> ProductsNeverSoldAsync(DateTime? startTime, DateTime? endTime, int pageIndex, int pageSize, bool includeHidden = false)
        //{
        //    var query1 = (from orderItem in _db.OrderItems.AsNoTracking()
        //                  join o in _db.Orders.AsNoTracking() on orderItem.OrderId equals o.Id
        //                  where (!startTime.HasValue || startTime.Value <= o.CreatedOnUtc)
        //                    && (!endTime.HasValue || endTime.Value >= o.CreatedOnUtc)
        //                  select orderItem.ProductId).Distinct();

        //    var query2 = from p in _db.Products.AsNoTracking()
        //                 where !query1.Contains(p.Id)
        //                    && !p.IsSystemProduct
        //                    && (includeHidden || p.Published)
        //                    && p.ProductTypeId != (int)ProductType.GroupedProduct
        //                 orderby p.Name
        //                 select p;

        //    return query2.ToPagedList(pageIndex, pageSize).LoadAsync();
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