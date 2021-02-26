//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using SixLabors.ImageSharp.ColorSpaces;
//using Smartstore.Collections;
//using Smartstore.Core.Catalog.Products;
//using Smartstore.Core.Checkout.Payment;
//using Smartstore.Core.Checkout.Shipping;
//using Smartstore.Core.Common.Services;
//using Smartstore.Core.Data;
//using StackExchange.Profiling.Internal;

//namespace Smartstore.Core.Checkout.Orders
//{
//    public partial class OrderReportService// : //IOrderReportService
//    {
//        private readonly SmartDbContext _db;
//        private readonly IDateTimeHelper _dateTimeHelper;

//        public OrderReportService(
//            SmartDbContext db,
//            IDateTimeHelper dateTimeHelper)
//        {
//            _db = db;
//            _dateTimeHelper = dateTimeHelper;
//        }

//        public virtual OrderAverageReportLine GetOrderAverageReportLine(
//            int storeId,
//            int[] orderStatusIds,
//            int[] paymentStatusIds,
//            int[] shippingStatusIds,
//            DateTime? startTimeUtc,
//            DateTime? endTimeUtc,
//            string billingEmail,
//            bool ignoreCancelledOrders = false)
//        {
//            var query = _db.Orders;
//            query = query.appl
                
                
//                .Where(o => !o.Deleted);

//            if (storeId > 0)
//            {
//                query = query.Where(o => o.StoreId == storeId);
//            }
//            if (ignoreCancelledOrders)
//            {
//                var cancelledOrderStatusId = (int)OrderStatus.Cancelled;
//                query = query.Where(o => o.OrderStatusId != cancelledOrderStatusId);
//            }
//            if (startTimeUtc.HasValue)
//            {
//                query = query.Where(o => startTimeUtc.Value <= o.CreatedOnUtc);
//            }
//            if (endTimeUtc.HasValue)
//            {
//                query = query.Where(o => endTimeUtc.Value >= o.CreatedOnUtc);
//            }
//            if (!string.IsNullOrEmpty(billingEmail))
//            {
//                query = query.Where(o => o.BillingAddress != null && !string.IsNullOrEmpty(o.BillingAddress.Email) && o.BillingAddress.Email.Contains(billingEmail));
//            }
//            if (orderStatusIds != null && orderStatusIds.Any())
//            {
//                query = query.Where(x => orderStatusIds.Contains(x.OrderStatusId));
//            }
//            if (paymentStatusIds != null && paymentStatusIds.Any())
//            {
//                query = query.Where(x => paymentStatusIds.Contains(x.PaymentStatusId));
//            }
//            if (shippingStatusIds != null && shippingStatusIds.Any())
//            {
//                query = query.Where(x => shippingStatusIds.Contains(x.ShippingStatusId));
//            }

//        //    return GetOrderAverageReportLine(query);
//        //}

//        public virtual async Task<OrderAverageReportLine> GetOrderAverageReportLineAsync(IQueryable<Order> orderQuery)
//        {
//            var item = await orderQuery.GroupBy(x => 1)
//                .Select(x => new OrderAverageReportLine
//                {
//                    CountOrders = x.Count(),
//                    SumTax = x.Sum(x => x.OrderTax),
//                    SumOrders = x.Sum(x => x.OrderTotal)
//                })
//                .FirstOrDefaultAsync();

//            return item ?? new();
//        }

//        //public virtual OrderAverageReportLineSummary OrderAverageReport(int storeId, OrderStatus os)
//        //{
//        //    var item = new OrderAverageReportLineSummary
//        //    {
//        //        OrderStatus = os
//        //    };

//        //    DateTime nowDt = _dateTimeHelper.ConvertToUserTime(DateTime.Now);
//        //    TimeZoneInfo timeZone = _dateTimeHelper.CurrentTimeZone;
//        //    var orderStatusId = new int[] { (int)os };

//        //    // Today.
//        //    DateTime t1 = new DateTime(nowDt.Year, nowDt.Month, nowDt.Day);
//        //    if (!timeZone.IsInvalidTime(t1))
//        //    {
//        //        DateTime? startTime1 = _dateTimeHelper.ConvertToUtcTime(t1, timeZone);
//        //        DateTime? endTime1 = null;
//        //        var todayResult = GetOrderAverageReportLine(storeId, orderStatusId, null, null, startTime1, endTime1, null);
//        //        item.SumTodayOrders = todayResult.SumOrders;
//        //        item.CountTodayOrders = todayResult.CountOrders;
//        //    }

//        //    // Week.
//        //    DayOfWeek fdow = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
//        //    DateTime today = new DateTime(nowDt.Year, nowDt.Month, nowDt.Day);
//        //    DateTime t2 = today.AddDays(-(today.DayOfWeek - fdow));
//        //    if (!timeZone.IsInvalidTime(t2))
//        //    {
//        //        DateTime? startTime2 = _dateTimeHelper.ConvertToUtcTime(t2, timeZone);
//        //        DateTime? endTime2 = null;
//        //        var weekResult = GetOrderAverageReportLine(storeId, orderStatusId, null, null, startTime2, endTime2, null);
//        //        item.SumThisWeekOrders = weekResult.SumOrders;
//        //        item.CountThisWeekOrders = weekResult.CountOrders;
//        //    }

//        //    // Month.
//        //    DateTime t3 = new DateTime(nowDt.Year, nowDt.Month, 1);
//        //    if (!timeZone.IsInvalidTime(t3))
//        //    {
//        //        DateTime? startTime3 = _dateTimeHelper.ConvertToUtcTime(t3, timeZone);
//        //        DateTime? endTime3 = null;
//        //        var monthResult = GetOrderAverageReportLine(storeId, orderStatusId, null, null, startTime3, endTime3, null);
//        //        item.SumThisMonthOrders = monthResult.SumOrders;
//        //        item.CountThisMonthOrders = monthResult.CountOrders;
//        //    }

//        //    // Year.
//        //    DateTime t4 = new DateTime(nowDt.Year, 1, 1);
//        //    if (!timeZone.IsInvalidTime(t4))
//        //    {
//        //        DateTime? startTime4 = _dateTimeHelper.ConvertToUtcTime(t4, timeZone);
//        //        DateTime? endTime4 = null;
//        //        var yearResult = GetOrderAverageReportLine(storeId, orderStatusId, null, null, startTime4, endTime4, null);
//        //        item.SumThisYearOrders = yearResult.SumOrders;
//        //        item.CountThisYearOrders = yearResult.CountOrders;
//        //    }

//        //    // All time.
//        //    DateTime? startTime5 = null;
//        //    DateTime? endTime5 = null;
//        //    var allTimeResult = GetOrderAverageReportLine(storeId, orderStatusId, null, null, startTime5, endTime5, null);
//        //    item.SumAllTimeOrders = allTimeResult.SumOrders;
//        //    item.CountAllTimeOrders = allTimeResult.CountOrders;

//        //    return item;
//        //}

//        public virtual IPagedList<BestsellersReportLine> BestSellersReport(
//            int storeId,
//            DateTime? startTime,
//            DateTime? endTime,
//            int? orderStatusId = null,
//            int? paymentStatusId,
//            int? shippingStatusId,
//            int? billingCountryId,
//            int pageIndex = 0,
//            int pageSize = int.MaxValue,
//            ReportSorting sorting = ReportSorting.ByQuantityDesc,
//            bool showHidden = false)
//        {

//            var query =
//                from orderItem in _db.OrderItems
//                join o in _db.Orders.AsNoTracking() on orderItem.OrderId equals o.Id
//                join p in _db.Products.AsNoTracking() on orderItem.ProductId equals p.Id;
//                //where  (storeId == 0 || storeId == o.StoreId)
//                //    && (!startTime.HasValue || startTime.Value <= o.CreatedOnUtc)
//                //    && (!endTime.HasValue || endTime.Value >= o.CreatedOnUtc)
//                //    && (!orderStatusId.HasValue || orderStatusId == o.OrderStatusId)
//                //    && (!paymentStatusId.HasValue || paymentStatusId == o.PaymentStatusId)
//                //    && (!shippingStatusId.HasValue || shippingStatusId == o.ShippingStatusId)
//                //    && (billingCountryId == 0 || o.BillingAddress.CountryId == billingCountryId)
//                //    && (!p.IsSystemProduct)
//                //    && (showHidden || p.Published)
//                //select orderItem;
//                ;
//            var xxx = query.appl


//            // Group by product ID.
//            var groupedQuery =
//                from orderItem in query
//                group orderItem by orderItem.ProductId into g
//                select new BestsellersReportLine
//                {
//                    ProductId = g.Key,
//                    TotalAmount = g.Sum(x => x.PriceExclTax),
//                    TotalQuantity = g.Sum(x => x.Quantity)
//                };

//            groupedQuery = sorting switch
//            {
//                ReportSorting.ByAmountAsc => groupedQuery.OrderBy(x => x.TotalAmount),
//                ReportSorting.ByAmountDesc => groupedQuery.OrderByDescending(x => x.TotalAmount),
//                ReportSorting.ByQuantityAsc => groupedQuery.OrderBy(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
//                _ => groupedQuery.OrderByDescending(x => x.TotalQuantity).ThenByDescending(x => x.TotalAmount),
//            };

//            return groupedQuery.ToPagedList(pageIndex, pageSize);
//        }

//        /// <summary>
//        /// So this retrieves product ids, which have also been purchased with productId
//        /// </summary>
//        /// <param name="productId"></param>
//        /// <param name="recordsToReturn"></param>
//        /// <param name="storeId"></param>
//        /// <param name="showHidden"></param>
//        /// <returns></returns>
//        public virtual async Task<int[]> GetAlsoPurchasedProductsIdsAsync(int productId, int? recordsToReturn = 5, int storeId = 0, bool showHidden = false)
//        {
//            if (productId == 0)
//                return Array.Empty<int>();

//            // This inner query retrieves all orderItems which contain the productId.
//            var orderItems = await _db.OrderItems
//                .Where(x => x.ProductId == productId)
//                .Select(x => x.OrderId)
//                .ToListAsync();

//            var query = _db.OrderItems
//                .Include(x => x.Order)                
//                .Join(_db.Products, x => x.ProductId, y => y.Id, (x, y) => new { OrderItem = x, Product = y })
//                .Where(x => orderItems.Contains(x.OrderItem.OrderId)                
//                    && x.Product.Id != productId
//                    && (showHidden || x.Product.Published)
//                    && (storeId == 0 || x.OrderItem.Order.StoreId == storeId)
//                    && !x.Product.IsSystemProduct)
//                .GroupBy(x => x.Product.Id)
//                .Select(x => new
//                {
//                    ProductId = x.Key,
//                    ProductsPurchased = x.Sum(x => x.OrderItem.Quantity)
//                })
//                .OrderByDescending(x => x.ProductsPurchased);

//            if (recordsToReturn.GetValueOrDefault() > 0)
//            {
//                query = query.Take(() => recordsToReturn.Value);
//            }

//            return await query
//                .Select(x => x.ProductId)
//                .ToArrayAsync();
//        }

//        public virtual IPagedList<Product> ProductsNeverSold(DateTime? startTime, DateTime? endTime, int pageIndex, int pageSize, bool showHidden = false)
//        {
//            var groupedProductId = (int)ProductType.GroupedProduct;

//            var query1 = (from orderItem in _orderItemRepository.TableUntracked
//                          join o in _orderRepository.TableUntracked on orderItem.OrderId equals o.Id
//                          where !o.Deleted &&
//                                (!startTime.HasValue || startTime.Value <= o.CreatedOnUtc) &&
//                                (!endTime.HasValue || endTime.Value >= o.CreatedOnUtc)
//                          select orderItem.ProductId).Distinct();

//            var query2 = from p in _productRepository.TableUntracked
//                         where !query1.Contains(p.Id) &&
//                                !p.Deleted && !p.IsSystemProduct &&
//                               (showHidden || p.Published) &&
//                                p.ProductTypeId != groupedProductId
//                         orderby p.Name
//                         select p;

//            var products = new PagedList<Product>(query2, pageIndex, pageSize);
//            return products;
//        }

//        public virtual decimal GetProfit(IQueryable<Order> orderQuery)
//        {
//            var query =
//                from orderItem in _orderItemRepository.Table
//                join o in orderQuery on orderItem.OrderId equals o.Id
//                select orderItem;

//            var productCost = Convert.ToDecimal(query.Sum(oi => (decimal?)oi.ProductCost * oi.Quantity));
//            var summary = GetOrderAverageReportLine(orderQuery);
//            var profit = summary.SumOrders - summary.SumTax - productCost;

//            return profit;
//        }

//        public virtual IPagedList<OrderDataPoint> GetIncompleteOrders(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc)
//        {
//            var query = _orderRepository.Table;
//            query = query.Where(x => !x.Deleted && x.OrderStatusId != (int)OrderStatus.Cancelled);

//            if (storeId > 0)
//            {
//                query = query.Where(x => x.StoreId == storeId);
//            }
//            if (startTimeUtc.HasValue)
//            {
//                query = query.Where(x => startTimeUtc.Value <= x.CreatedOnUtc);
//            }
//            if (endTimeUtc.HasValue)
//            {
//                query = query.Where(x => endTimeUtc.Value >= x.CreatedOnUtc);
//            }

//            query = query.Where(x =>
//                x.ShippingStatusId == (int)ShippingStatus.NotYetShipped
//                || x.PaymentStatusId == (int)PaymentStatus.Pending
//            );
//            var dataPoints = query.Select(x => new OrderDataPoint
//            {
//                CreatedOn = x.CreatedOnUtc,
//                OrderTotal = x.OrderTotal,
//                OrderStatusId = x.OrderStatusId,
//                PaymentStatusId = x.PaymentStatusId,
//                ShippingStatusId = x.ShippingStatusId
//            });

//            return new PagedList<OrderDataPoint>(dataPoints, 0, int.MaxValue);
//        }

//        public virtual IPagedList<OrderDataPoint> GetOrdersDashboardData(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc, int pageIndex, int pageSize)
//        {
//            var query = _orderRepository.Table;
//            query = query.Where(x => !x.Deleted);

//            if (storeId > 0)
//            {
//                query = query.Where(x => x.StoreId == storeId);
//            }
//            if (startTimeUtc.HasValue)
//            {
//                query = query.Where(x => startTimeUtc.Value <= x.CreatedOnUtc);
//            }
//            if (endTimeUtc.HasValue)
//            {
//                query = query.Where(x => endTimeUtc.Value >= x.CreatedOnUtc);
//            }

//            var dataPoints = query.Select(x => new OrderDataPoint
//            {
//                CreatedOn = x.CreatedOnUtc,
//                OrderTotal = x.OrderTotal,
//                OrderStatusId = x.OrderStatusId
//            });
//            return new PagedList<OrderDataPoint>(dataPoints, pageIndex, pageSize);
//        }

//        public virtual decimal GetOrdersTotal(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc)
//        {
//            var query = _orderRepository.Table;
//            query = query.Where(x => !x.Deleted);

//            if (storeId > 0)
//            {
//                query = query.Where(x => x.StoreId == storeId);
//            }
//            if (startTimeUtc.HasValue)
//            {
//                query = query.Where(x => startTimeUtc.Value <= x.CreatedOnUtc);
//            }
//            if (endTimeUtc.HasValue)
//            {
//                query = query.Where(x => endTimeUtc.Value >= x.CreatedOnUtc);
//            }

//            return query.Sum(x => (decimal?)x.OrderTotal) ?? decimal.Zero;
//        }
//    }
//}