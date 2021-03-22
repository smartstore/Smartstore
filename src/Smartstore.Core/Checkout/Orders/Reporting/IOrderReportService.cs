using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Orders.Reporting
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
  //      /// <summary>
  //      /// Gets an <see cref="OrderAverageReportLine"/> from <see cref="Orders"/> query.
  //      /// </summary>
  //      /// <param name="orderQuery"><see cref="IQueryable"/> of <see cref="Order"/>.</param>
  //      /// <returns><see cref="OrderAverageReportLine"/></returns>
  //      Task<OrderAverageReportLine> GetOrderAverageReportLineAsync(IQueryable<Order> orderQuery);

  //      /// <summary>
  //      /// Gets bestsellers report.
  //      /// </summary>
		///// <param name="storeId"><see cref="Store"/> identifier.</param>
  //      /// <param name="startTime"><see cref="Order"/> start <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="endTime"><see cref="Order"/> end <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="orderStatusId"><see cref="OrderStatus"/> identifier.</param>
  //      /// <param name="paymentStatusId"><see cref="PaymentStatus"/> identifier.</param>
  //      /// <param name="shippingStatusId"><see cref="ShippingStatus"/> identifier.</param>
  //      /// <param name="billingCountryId">Billing country identifier.</param>
  //      /// <param name="pageIndex">Page index.</param>
  //      /// <param name="pageSize">Page size.</param>
  //      /// <param name="sorting">Sorting of report items.</param>
  //      /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
  //      /// <returns>Best selling <see cref="Product"/>s.</returns>
		//Task<IPagedList<BestsellersReportLine>> BestsellersReportAsync(
  //          int storeId,
  //          DateTime? startTime,
  //          DateTime? endTime,
  //          int? orderStatusId = null,
  //          int? paymentStatusId = null,
  //          int? shippingStatusId = null,
  //          int? billingCountryId = null,
  //          int pageIndex = 0,
  //          int pageSize = int.MaxValue,
  //          ReportSorting sorting = ReportSorting.ByQuantityDesc,
  //          bool includeHidden = false);

  //      /// <summary>
  //      /// Gets an <see cref="Array"/> of <see cref="Product"/> identifiers purchased by other <see cref="Customer"/>s who purchased <paramref name="productId"/>.
  //      /// </summary>
  //      /// <param name="productId"><see cref="Product"/> identifier.</param>
  //      /// <param name="recordsToReturn">Number of records to return.</param>
		///// <param name="storeId"><see cref="Store"/> identifier.</param>
  //      /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
  //      /// <returns><see cref="Array"/> of <see cref="Product"/> identifiers.</returns>
		//Task<int[]> GetAlsoPurchasedProductIdsAsync(int productId, int? recordsToReturn = 5, int storeId = 0, bool includeHidden = false);

  //      /// <summary>
  //      /// Gets an <see cref="IPagedList{}"/> of <see cref="Product"/>s that have never been sold.
  //      /// </summary>
  //      /// <param name="startTime"><see cref="Order"/> start <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="endTime"><see cref="Order"/> end <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="pageIndex">Page index.</param>
  //      /// <param name="pageSize">Page size.</param>
  //      /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
  //      /// <returns><see cref="IPagedList{}"/> of <see cref="Product"/>s that have never been sold.</returns>
  //      Task<IPagedList<Product>> ProductsNeverSoldAsync(DateTime? startTime, DateTime? endTime, int pageIndex, int pageSize, bool includeHidden = false);

  //      /// <summary>
  //      /// Gets <see cref="Order"/> profit.
  //      /// </summary>
  //      /// <param name="orderQuery"><see cref="IQueryable"/> of <see cref="Order"/>.</param>
  //      /// <returns><see cref="Order"/> profit.</returns>
  //      Task<decimal> GetProfitAsync(IQueryable<Order> orderQuery);

  //      /// <summary>
  //      /// Gets <see cref="IPagedList{}"/> of incomplete <see cref="Order"/>s.
  //      /// </summary>
  //      /// <param name="storeId"><see cref="Store"/> identifier.</param>
  //      /// <param name="startTimeUtc"><see cref="Order"/> start <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="endTimeUtc"><see cref="Order"/> end <see cref="DateTime"/> as UTC.</param>
  //      /// <returns><see cref="IPagedList{}"/> of incomplete <see cref="Order"/>s.</returns>
  //      Task<IPagedList<OrderDataPoint>> GetIncompleteOrdersAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc);

  //      /// <summary>
  //      /// Gets <see cref="IPagedList{}"/> of <see cref="Order"/> as <see cref="OrderDataPoint"/>.
  //      /// </summary>
  //      /// <param name="storeId"><see cref="Store"/> identifier.</param>
  //      /// <param name="startTimeUtc"><see cref="Order"/> start <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="endTimeUtc"><see cref="Order"/> end <see cref="DateTime"/> as UTC.</param>
  //      /// <param name="pageIndex">Page index.</param>
  //      /// <param name="pageSize">Page size.</param>
  //      /// <returns><see cref="IPagedList{}"/> of <see cref="OrderDataPoint"/></returns>
  //      Task<IPagedList<OrderDataPoint>> GetOrdersDashboardDataAsync(int storeId, DateTime? startTimeUtc, DateTime? endTimeUtc, int pageIndex, int pageSize);
    }
}