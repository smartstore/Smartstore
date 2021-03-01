using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Checkout.Orders
{
    public enum ReportSorting
    {
        ByQuantityAsc = 0,
        ByQuantityDesc,
        ByAmountAsc,
        ByAmountDesc
    }

    /// <summary>
    /// Order report service interface.
    /// </summary>
    public partial interface IOrderReportService
    {

        /// <summary>
        /// Gets order average report.
        /// </summary>
        /// <param name="orderQuery">Order queryable.</param>
        /// <returns><see cref="OrderAverageReportLine"/>.</returns>
        Task<OrderAverageReportLine> GetOrderAverageReportLineAsync(IQueryable<Order> orderQuery);

        /// <summary>
        /// Gets best sellers report.
        /// </summary>
		/// <param name="storeId">Store identifier</param>
        /// <param name="startTime">Order start time; null to load all</param>
        /// <param name="endTime">Order end time; null to load all</param>
        /// <param name="orderStatus">Order status; null to load all records</param>
        /// <param name="paymentStatus">Order payment status; null to load all records</param>
        /// <param name="shippingStatus">Shipping status; null to load all records</param>
        /// <param name="billingCountryId">Billing country identifier; 0 to load all records</param>
        /// <param name="pageIndex">Page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="sorting">Sorting of report items.</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Best selling products.</returns>
		Task<IPagedList<BestsellersReportLine>> BestSellersReportAsync(
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
            bool showHidden = false);

        /// <summary>
        /// Gets a list of product identifiers purchased by other customers who purchased the above
        /// </summary>
		/// <param name="storeId">Store identifier</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="recordsToReturn">Records to return</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product collection</returns>
		Task<int[]> GetAlsoPurchasedProductsIdsAsync(int productId, int? recordsToReturn = 5, int storeId = 0, bool showHidden = false);

        /// <summary>
        /// Gets a list of products that were never sold
        /// </summary>
        /// <param name="startTime">Order start time; null to load all</param>
        /// <param name="endTime">Order end time; null to load all</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Products</returns>
        Task<IPagedList<Product>> ProductsNeverSoldAsync(DateTime? startTime, DateTime? endTime, int pageIndex, int pageSize, bool showHidden = false);

        /// <summary>
        /// Get order profit.
        /// </summary>
        /// <param name="orderQuery">Order queryable.</param>
        /// <returns>Order profit.</returns>
        Task<decimal> GetProfitAsync(IQueryable<Order> orderQuery);


        /// <summary>
        /// Get paged list of incomplete orders
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="startTimeUtc">Start time limitation</param>
        /// <param name="endTimeUtc">End time limitation</param>
        /// <returns>List of incomplete orders</returns>
        Task<IPagedList<OrderDataPoint>> GetIncompleteOrdersAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc);

        /// <summary>
        /// Get paged list of orders as ChartDataPoints
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="startTimeUtc">Start time UTC</param>
        /// <param name="endTimeUtc">End time UTC</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns></returns>
        Task<IPagedList<OrderDataPoint>> GetOrdersDashboardDataAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc, int pageIndex, int pageSize);
    }
}