using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    // TODO: (mg) DRY. See similar code in DashboardRegisteredCustomersViewComponent.

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
            var userTime = _dateTimeHelper.ConvertToUserTime(utcNow, DateTimeKind.Utc).Date;
            var startDate = (utcNow.Date - beginningOfYear).Days < 28 ? utcNow.AddDays(-27).Date : beginningOfYear;
            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;

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

            var model = new List<DashboardChartReportModel>
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

            // Format and sum values
            for (int i = 0; i < model.Count; i++)
            {
                foreach (var data in model[i].DataSets)
                {
                    for (int j = 0; j < data.Amount.Length; j++)
                    {
                        data.AmountFormatted[j] = primaryCurrency.AsMoney(data.Amount[j]).ToString();
                        data.QuantityFormatted[j] = data.Quantity[j].ToString("N0");
                    }

                    data.TotalAmount = data.Amount.Sum();
                    data.TotalAmountFormatted = primaryCurrency.AsMoney(data.TotalAmount).ToString();
                }

                model[i].TotalAmount = model[i].DataSets.Sum(x => x.TotalAmount);
                model[i].TotalAmountFormatted = primaryCurrency.AsMoney(model[i].TotalAmount).ToString();

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
            for (var i = 0; i < model.Count; i++)
            {
                var m = model[i];
                decimal totalBefore = 0;
                DateTime from = DateTime.MinValue;
                DateTime to = DateTime.MinValue;

                switch (i)
                {
                    // Yesterday.
                    case 0:
                        totalBefore = model[1].TotalAmount;
                        break;
                    // Order total for day before yesterday.
                    case 1:
                        from = utcNow.Date.AddDays(-2);
                        to = utcNow.Date.AddDays(-1);
                        totalBefore = orderDataPoints.Where(x => x.CreatedOn >= from && x.CreatedOn < to).Sum(x => x.OrderTotal);
                        break;
                    // Order total for week before.
                    case 2:
                        from = utcNow.Date.AddDays(-14);
                        to = utcNow.Date.AddDays(-7);
                        totalBefore = orderDataPoints.Where(x => x.CreatedOn >= from && x.CreatedOn < to).Sum(x => x.OrderTotal);
                        break;
                    // Order total for month before.
                    case 3:
                        from = utcNow.Date.AddDays(-56);
                        to = utcNow.Date.AddDays(-28);
                        totalBefore = await _db.Orders.ApplyAuditDateFilter(from, to).GetOrdersTotalAsync();
                        break;
                    // Order total for year before.
                    case 4:
                        from = beginningOfYear.AddYears(-1);
                        to = utcNow.AddYears(-1);
                        totalBefore = await _db.Orders.ApplyAuditDateFilter(from, to).GetOrdersTotalAsync();
                        break;
                };

                m.PercentageDelta = m.TotalAmount != 0 && totalBefore != 0
                    ? (int)Math.Round(m.TotalAmount / totalBefore * 100 - 100)
                    : 0;

                if (from != DateTime.MinValue && m.PercentageDelta != 0)
                {
                    var percentageStr = (m.PercentageDelta > 0 ? '+' : '-') + Math.Abs(m.PercentageDelta).ToString() + '%';
                    var fromStr = _dateTimeHelper.ConvertToUserTime(from, DateTimeKind.Utc).ToShortDateString();
                    var toStr = _dateTimeHelper.ConvertToUserTime(to, DateTimeKind.Utc).ToShortDateString();
                    
                    m.PercentageDescription = T("Admin.Report.ChangeComparedTo", percentageStr, fromStr, toStr);
                }
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
