using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardIncompleteOrdersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;

        public DashboardIncompleteOrdersViewComponent(SmartDbContext db, IDateTimeHelper dateTimeHelper)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                return Empty();
            }

            var model = new List<DashboardIncompleteOrdersModel>()
            {
                // Today = index 0
                new DashboardIncompleteOrdersModel(),
                // This week = index 1
                new DashboardIncompleteOrdersModel(),
                // This month = index 2
                new DashboardIncompleteOrdersModel(),
                // This year = index 3
                new DashboardIncompleteOrdersModel(),
            };

            // Query to get all incomplete orders of at least the last 28 days (if year is younger)
            var utcNow = DateTime.UtcNow;
            var beginningOfYear = new DateTime(utcNow.Year, 1, 1);
            var startDate = (utcNow.Date - beginningOfYear).Days < 28 ? utcNow.AddDays(-28).Date : beginningOfYear;
            var dataPoints = await _db.Orders
                .AsNoTracking()
                .ApplyAuditDateFilter(startDate, null)
                .ApplyIncompleteOrdersFilter()
                .Select(x => new OrderDataPoint
                {
                    CreatedOn = x.CreatedOnUtc,
                    OrderTotal = x.OrderTotal,
                    OrderStatusId = x.OrderStatusId,
                    PaymentStatusId = x.PaymentStatusId,
                    ShippingStatusId = x.ShippingStatusId
                })
                .ToListAsync();

            var userTime = _dateTimeHelper.ConvertToUserTime(utcNow, DateTimeKind.Utc);
            // Sort pending orders by status and period
            foreach (var dataPoint in dataPoints)
            {
                dataPoint.CreatedOn = _dateTimeHelper.ConvertToUserTime(dataPoint.CreatedOn, DateTimeKind.Utc);
                if (dataPoint.ShippingStatusId == (int)ShippingStatus.NotYetShipped)
                {
                    // Not Shipped = index 0
                    IncompleteOrdersReportAddData(dataPoint, model, 0);
                }
                if (dataPoint.PaymentStatusId == (int)PaymentStatus.Pending)
                {
                    // Not paid = index 1
                    IncompleteOrdersReportAddData(dataPoint, model, 1);
                }
                if (dataPoint.OrderStatusId == (int)OrderStatus.Pending)
                {
                    // New Order = index 2
                    IncompleteOrdersReportAddData(dataPoint, model, 2);
                }

                // Calculate orders Total for periods
                // Today
                if (dataPoint.CreatedOn >= userTime.Date)
                {
                    IncompleteOrdersReportAddTotal(dataPoint, model, 0);
                }
                // This week
                else if (dataPoint.CreatedOn >= userTime.AddDays(-6).Date)
                {
                    IncompleteOrdersReportAddTotal(dataPoint, model, 1);
                }
                // This month 
                else if (dataPoint.CreatedOn >= userTime.AddDays(-27).Date)
                {
                    IncompleteOrdersReportAddTotal(dataPoint, model, 2);
                }
                // This year 
                else if (dataPoint.CreatedOn.Year == userTime.Year)
                {
                    IncompleteOrdersReportAddTotal(dataPoint, model, 3);
                }
            }

            foreach (var report in model)
            {
                report.QuantityTotal = report.Quantity.ToString("N0");
                report.AmountTotal = Services.CurrencyService.PrimaryCurrency.AsMoney(report.Amount);
                for (int i = 0; i < report.Data.Count; i++)
                {
                    var data = report.Data[i];
                    data.QuantityFormatted = data.Quantity.ToString("N0");
                    data.AmountFormatted = Services.CurrencyService.PrimaryCurrency.AsMoney(data.Amount);
                }
            }

            return View(model);
        }

        protected void IncompleteOrdersReportAddData(OrderDataPoint dataPoint, List<DashboardIncompleteOrdersModel> reports, int dataIndex)
        {
            var userTime = _dateTimeHelper.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Utc);

            // Within this year
            if (dataPoint.CreatedOn.Year == userTime.Year)
            {
                var year = reports[reports.Count - 1].Data[dataIndex];
                year.Quantity++;
                year.Amount += dataPoint.OrderTotal;
            }

            // Today
            if (dataPoint.CreatedOn >= userTime.Date)
            {
                // Apply data to all periods (but year)
                for (int i = 0; i < reports.Count - 1; i++)
                {
                    var today = reports[i].Data[dataIndex];
                    today.Amount += dataPoint.OrderTotal;
                    today.Quantity++;
                }
            }
            // Within last 7 days
            else if (dataPoint.CreatedOn >= userTime.AddDays(-6).Date)
            {
                // Apply data to week and month periods
                for (int i = 1; i < reports.Count - 1; i++)
                {
                    var week = reports[i].Data[dataIndex];
                    week.Amount += dataPoint.OrderTotal;
                    week.Quantity++;
                }
            }
            // Within last 28 days
            else if (dataPoint.CreatedOn >= userTime.AddDays(-27).Date)
            {
                var month = reports[2].Data[dataIndex];
                month.Amount += dataPoint.OrderTotal;
                month.Quantity++;
            }
        }

        [NonAction]
        protected void IncompleteOrdersReportAddTotal(OrderDataPoint dataPoint, List<DashboardIncompleteOrdersModel> reports, int periodState)
        {
            for (int i = periodState; i < reports.Count; i++)
            {
                reports[i].Quantity++;
                reports[i].Amount += dataPoint.OrderTotal;
            }
        }
    }
}
