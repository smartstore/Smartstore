using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardIncompleteOrdersViewComponent : DashboardViewComponentBase
    {
        private readonly SmartDbContext _db;

        public DashboardIncompleteOrdersViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public override async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                return Empty();
            }

            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;

            var model = new List<DashboardIncompleteOrdersModel>
            {
                // Index 0: today.
                new(),
                // Index 1: this week.
                new(),
                // Index 2: this month.
                new(),
                // Index 3: this year.
                new()
            };

            var dataPoints = await _db.Orders
                .AsNoTracking()
                .ApplyAuditDateFilter(CreatedFrom, null)
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

            // Sort pending orders by status and period.
            foreach (var dataPoint in dataPoints)
            {
                dataPoint.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(dataPoint.CreatedOn, DateTimeKind.Utc);

                if (dataPoint.ShippingStatusId == (int)ShippingStatus.NotYetShipped)
                {
                    // Index 0: not Shipped.
                    IncompleteOrdersReportAddData(dataPoint, model, 0);
                }
                if (dataPoint.PaymentStatusId == (int)PaymentStatus.Pending)
                {
                    // Index 1: not paid.
                    IncompleteOrdersReportAddData(dataPoint, model, 1);
                }
                if (dataPoint.OrderStatusId == (int)OrderStatus.Pending)
                {
                    // Index 2: new order.
                    IncompleteOrdersReportAddData(dataPoint, model, 2);
                }

                // Calculate orders Total for periods.
                if (dataPoint.CreatedOn >= UserTime.Date)
                {
                    // Today.
                    AddTotalToIncompleteOrdersReport(dataPoint, 0);
                }
                else if (dataPoint.CreatedOn >= UserTime.AddDays(-6).Date)
                {
                    // This week.
                    AddTotalToIncompleteOrdersReport(dataPoint, 1);
                }
                else if (dataPoint.CreatedOn >= UserTime.AddDays(-27).Date)
                {
                    // This month.
                    AddTotalToIncompleteOrdersReport(dataPoint, 2);
                }
                else if (dataPoint.CreatedOn.Year == UserTime.Year)
                {
                    // This year.
                    AddTotalToIncompleteOrdersReport(dataPoint, 3);
                }
            }

            foreach (var report in model)
            {
                report.QuantityTotal = report.Quantity.ToString("N0");
                report.AmountTotal = Services.CurrencyService.CreateMoney(report.Amount, primaryCurrency);

                for (var i = 0; i < report.Data.Count; i++)
                {
                    var data = report.Data[i];
                    data.QuantityFormatted = data.Quantity.ToString("N0");
                    data.AmountFormatted = Services.CurrencyService.CreateMoney(data.Amount, primaryCurrency);
                }
            }

            return View(model);

            void AddTotalToIncompleteOrdersReport(OrderDataPoint dataPoint, int periodState)
            {
                for (var i = periodState; i < model.Count; i++)
                {
                    model[i].Quantity++;
                    model[i].Amount += dataPoint.OrderTotal;
                }
            }
        }

        protected void IncompleteOrdersReportAddData(OrderDataPoint dataPoint, List<DashboardIncompleteOrdersModel> reports, int dataIndex)
        {
            if (dataPoint.CreatedOn.Year == UserTime.Year)
            {
                // This year.
                var year = reports[reports.Count - 1].Data[dataIndex];
                year.Quantity++;
                year.Amount += dataPoint.OrderTotal;
            }

            if (dataPoint.CreatedOn >= UserTime.Date)
            {
                // Today. Apply data to all periods (but year).
                for (var i = 0; i < reports.Count - 1; i++)
                {
                    var today = reports[i].Data[dataIndex];
                    today.Amount += dataPoint.OrderTotal;
                    today.Quantity++;
                }
            }
            else if (dataPoint.CreatedOn >= UserTime.AddDays(-6).Date)
            {
                // Last 7 days. Apply data to week and month periods.
                for (var i = 1; i < reports.Count - 1; i++)
                {
                    var week = reports[i].Data[dataIndex];
                    week.Amount += dataPoint.OrderTotal;
                    week.Quantity++;
                }
            }
            else if (dataPoint.CreatedOn >= UserTime.AddDays(-27).Date)
            {
                // Last 28 days.
                var month = reports[2].Data[dataIndex];
                month.Amount += dataPoint.OrderTotal;
                month.Quantity++;
            }
        }
    }
}
