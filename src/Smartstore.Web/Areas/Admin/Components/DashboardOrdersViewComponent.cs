using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardOrdersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;

        public DashboardOrdersViewComponent(SmartDbContext db, IDateTimeHelper dateTimeHelper)
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

            // Get orders of at least last 28 days (if year is younger)
            var utcNow = DateTime.UtcNow;
            var beginningOfYear = new DateTime(utcNow.Year, 1, 1);
            var startDate = (utcNow.Date - beginningOfYear).Days < 28 ? utcNow.AddDays(-27).Date : beginningOfYear;
            var orderDataPoints = await _db.Orders
                .AsNoTracking()
                .ApplyAuditDateFilter(startDate, null)
                .Select(x => new OrderDataPoint
                {
                    CreatedOn = x.CreatedOnUtc,
                    OrderTotal = x.OrderTotal,
                    OrderStatusId = x.OrderStatusId
                })
                .ToListAsync();

            var model = new List<DashboardChartReportModel>()
            {
                // Today = index 0
                new DashboardChartReportModel(4, 24),
                // Yesterday = index 1
                new DashboardChartReportModel(4, 24),
                // Last 7 days = index 2
                new DashboardChartReportModel(4, 7),
                // Last 28 days = index 3
                new DashboardChartReportModel(4, 4),
                // This year = index 4
                new DashboardChartReportModel(4, 12),
            };

            foreach (var dataPoint in orderDataPoints)
            {
                dataPoint.CreatedOn = _dateTimeHelper.ConvertToUserTime(dataPoint.CreatedOn, DateTimeKind.Utc);
                SetOrderReportData(model, dataPoint);
            }

            var userTime = _dateTimeHelper.ConvertToUserTime(utcNow, DateTimeKind.Utc).Date;
            // Format and sum values
            for (int i = 0; i < model.Count; i++)
            {
                foreach (var data in model[i].DataSets)
                {
                    for (int j = 0; j < data.Amount.Length; j++)
                    {
                        data.AmountFormatted[j] = Services.CurrencyService.PrimaryCurrency.AsMoney(data.Amount[j]).ToString();
                        data.QuantityFormatted[j] = data.Quantity[j].ToString("N0");
                    }

                    data.TotalAmount = data.Amount.Sum();
                    data.TotalAmountFormatted = Services.CurrencyService.PrimaryCurrency.AsMoney(data.TotalAmount).ToString();
                }

                model[i].TotalAmount = model[i].DataSets.Sum(x => x.TotalAmount);
                model[i].TotalAmountFormatted = Services.CurrencyService.PrimaryCurrency.AsMoney(model[i].TotalAmount).ToString();

                // Create labels for all dataPoints
                for (int j = 0; j < model[i].Labels.Length; j++)
                {
                    // Today & yesterday
                    if (i <= 1)
                    {
                        model[i].Labels[j] = userTime.AddHours(j).ToString("t") + " - " + userTime.AddHours(j).AddMinutes(59).ToString("t");
                    }
                    // Last 7 days
                    else if (i == 2)
                    {
                        model[i].Labels[j] = userTime.AddDays(-6 + j).ToString("m");
                    }
                    // Last 28 days
                    else if (i == 3)
                    {
                        var fromDay = -(7 * model[i].Labels.Length);
                        var toDayOffset = j == model[i].Labels.Length - 1 ? 0 : 1;
                        model[i].Labels[j] = userTime.AddDays(fromDay + 7 * j).ToString("m") + " - " + userTime.AddDays(fromDay + 7 * (j + 1) - toDayOffset).ToString("m");
                    }
                    // This year
                    else if (i == 4)
                    {
                        model[i].Labels[j] = new DateTime(userTime.Year, j + 1, 1).ToString("Y");
                    }
                }
            }

            // Get sum of orders for corresponding periods to calculate change in percentage.
            var sumBefore = new decimal[]
            {
                model[1].TotalAmount,
                
                // Get orders count for day before yesterday.
                orderDataPoints.Where( x =>
                    x.CreatedOn >= utcNow.Date.AddDays(-2) && x.CreatedOn < utcNow.Date.AddDays(-1)
                ).Sum(x => x.OrderTotal),
                
                // Get orders count for week before.
                orderDataPoints.Where( x =>
                    x.CreatedOn >= utcNow.Date.AddDays(-14) && x.CreatedOn < utcNow.Date.AddDays(-7)
                ).Sum(x => x.OrderTotal),

                // Get orders count for month.
                await _db.Orders.ApplyAuditDateFilter(beginningOfYear.AddDays(-56), utcNow.Date.AddDays(-28)).GetOrdersTotalAsync(),

                // Get orders count for year.
                await _db.Orders.ApplyAuditDateFilter(beginningOfYear.AddYears(-1), utcNow.AddYears(-1)).GetOrdersTotalAsync()
            };

            // Format percentage value
            for (int i = 0; i < model.Count; i++)
            {
                model[i].PercentageDelta = model[i].TotalAmount != 0 && sumBefore[i] != 0
                    ? (int)Math.Round(model[i].TotalAmount / sumBefore[i] * 100 - 100)
                    : 0;
            }

            return View(model);
        }

        protected void SetOrderReportData(List<DashboardChartReportModel> reports, OrderDataPoint dataPoint)
        {
            var userTime = _dateTimeHelper.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Utc);
            var dataIndex = dataPoint.OrderStatusId == 40 ? 0 : dataPoint.OrderStatusId / 10;

            // Today
            if (dataPoint.CreatedOn >= userTime.Date)
            {
                var today = reports[0].DataSets[dataIndex];
                today.Amount[dataPoint.CreatedOn.Hour] += dataPoint.OrderTotal;
                today.Quantity[dataPoint.CreatedOn.Hour]++;
            }
            // Yesterday
            else if (dataPoint.CreatedOn >= userTime.AddDays(-1).Date)
            {
                var yesterday = reports[1].DataSets[dataIndex];
                yesterday.Amount[dataPoint.CreatedOn.Hour] += dataPoint.OrderTotal;
                yesterday.Quantity[dataPoint.CreatedOn.Hour]++;
            }

            // Within last 7 days
            if (dataPoint.CreatedOn >= userTime.AddDays(-6).Date)
            {
                var week = reports[2].DataSets[dataIndex];
                var weekIndex = (userTime.Date - dataPoint.CreatedOn.Date).Days;
                week.Amount[week.Amount.Length - weekIndex - 1] += dataPoint.OrderTotal;
                week.Quantity[week.Quantity.Length - weekIndex - 1]++;
            }

            // Within last 28 days
            if (dataPoint.CreatedOn >= userTime.AddDays(-27).Date)
            {
                var month = reports[3].DataSets[dataIndex];
                var monthIndex = (userTime.Date - dataPoint.CreatedOn.Date).Days / 7;
                month.Amount[month.Amount.Length - monthIndex - 1] += dataPoint.OrderTotal;
                month.Quantity[month.Quantity.Length - monthIndex - 1]++;
            }

            // Within this year
            if (dataPoint.CreatedOn.Year == userTime.Year)
            {
                var year = reports[4].DataSets[dataIndex];
                year.Amount[dataPoint.CreatedOn.Month - 1] += dataPoint.OrderTotal;
                year.Quantity[dataPoint.CreatedOn.Month - 1]++;
            }
        }
    }
}
