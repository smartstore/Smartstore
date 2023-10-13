using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardOrdersViewComponent : DashboardViewComponentBase
    {
        private readonly SmartDbContext _db;

        public DashboardOrdersViewComponent(SmartDbContext db)
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

            var orderDataPoints = await _db.Orders
                .AsNoTracking()
                .ApplyAuditDateFilter(CreatedFrom, null)
                .Select(x => new OrderDataPoint
                {
                    CreatedOn = x.CreatedOnUtc,
                    OrderTotal = x.OrderTotal,
                    OrderStatusId = x.OrderStatusId
                })
                .ToListAsync();

            var model = DashboardChartReportModel.Create(4);

            // Sort data for chart display.
            foreach (var dataPoint in orderDataPoints)
            {
                dataPoint.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(dataPoint.CreatedOn, DateTimeKind.Utc);
                SetOrderReportData(model, dataPoint);
            }

            for (var i = 0; i < model.Count; i++)
            {
                var m = model[i];

                // Format and sum values.
                foreach (var data in m.DataSets)
                {
                    for (var j = 0; j < data.Amount.Length; j++)
                    {
                        data.AmountFormatted[j] = Services.CurrencyService.CreateMoney(data.Amount[j], primaryCurrency).ToString();
                        data.QuantityFormatted[j] = data.Quantity[j].ToString("N0");
                    }

                    data.TotalAmount = data.Amount.Sum();
                    data.TotalAmountFormatted = Services.CurrencyService.CreateMoney(data.TotalAmount, primaryCurrency).ToString();
                }

                m.TotalAmount = m.DataSets.Sum(x => x.TotalAmount);
                m.TotalAmountFormatted = Services.CurrencyService.CreateMoney(m.TotalAmount, primaryCurrency).ToString();

                // Create labels for data points.
                for (var j = 0; j < m.Labels.Length; j++)
                {
                    // Today & yesterday.
                    if (i <= 1)
                    {
                        m.Labels[j] = UserTime.Date.AddHours(j).ToString("t") + " - " + UserTime.Date.AddHours(j).AddMinutes(59).ToString("t");
                    }
                    // Last 7 days.
                    else if (i == 2)
                    {
                        m.Labels[j] = UserTime.Date.AddDays(-6 + j).ToString("m");
                    }
                    // Last 28 days.
                    else if (i == 3)
                    {
                        var fromDay = -(7 * m.Labels.Length);
                        var toDayOffset = j == m.Labels.Length - 1 ? 0 : 1;
                        m.Labels[j] = UserTime.Date.AddDays(fromDay + 7 * j).ToString("m") + " - " + UserTime.Date.AddDays(fromDay + 7 * (j + 1) - toDayOffset).ToString("m");
                    }
                    // This year.
                    else if (i == 4)
                    {
                        m.Labels[j] = new DateTime(UserTime.Date.Year, j + 1, 1).ToString("Y");
                    }
                }

                // Get sum of orders for corresponding periods to calculate change in percentage.
                if (i == 1 || i == 2)
                {
                    // Day before yesterday or week before.
                    m.ComparedTotalAmount = orderDataPoints.Where(x => x.CreatedOn >= m.ComparedFrom && x.CreatedOn < m.ComparedTo).Sum(x => x.OrderTotal);
                    ApplyComparedToDescription(m);
                }
                else if (i == 3 || i == 4)
                {
                    // Month or year before.
                    m.ComparedTotalAmount = await _db.Orders.ApplyAuditDateFilter(m.ComparedFrom, m.ComparedTo).GetOrdersTotalAsync();
                    ApplyComparedToDescription(m);
                }
            }

            // Yesterday.
            model[0].ComparedTotalAmount = model[1].TotalAmount;
            ApplyComparedToDescription(model[0]);

            return View(model);
        }

        protected void SetOrderReportData(List<DashboardChartReportModel> reports, OrderDataPoint dataPoint)
        {
            var dataIndex = dataPoint.OrderStatusId == 40 ? 0 : dataPoint.OrderStatusId / 10;

            if (dataPoint.CreatedOn >= UserTime.Date)
            {
                var today = reports[0].DataSets[dataIndex];
                today.Amount[dataPoint.CreatedOn.Hour] += dataPoint.OrderTotal;
                today.Quantity[dataPoint.CreatedOn.Hour]++;
            }
            else if (dataPoint.CreatedOn >= UserTime.AddDays(-1).Date)
            {
                var yesterday = reports[1].DataSets[dataIndex];
                yesterday.Amount[dataPoint.CreatedOn.Hour] += dataPoint.OrderTotal;
                yesterday.Quantity[dataPoint.CreatedOn.Hour]++;
            }

            if (dataPoint.CreatedOn >= UserTime.AddDays(-6).Date)
            {
                // Last 7 days.
                var week = reports[2].DataSets[dataIndex];
                var weekIndex = (UserTime.Date - dataPoint.CreatedOn.Date).Days;
                week.Amount[week.Amount.Length - weekIndex - 1] += dataPoint.OrderTotal;
                week.Quantity[week.Quantity.Length - weekIndex - 1]++;
            }

            if (dataPoint.CreatedOn >= UserTime.AddDays(-27).Date)
            {
                // Last 28 days.
                var month = reports[3].DataSets[dataIndex];
                var monthIndex = (UserTime.Date - dataPoint.CreatedOn.Date).Days / 7;
                month.Amount[month.Amount.Length - monthIndex - 1] += dataPoint.OrderTotal;
                month.Quantity[month.Quantity.Length - monthIndex - 1]++;
            }

            if (dataPoint.CreatedOn.Year == UserTime.Year)
            {
                var year = reports[4].DataSets[dataIndex];
                year.Amount[dataPoint.CreatedOn.Month - 1] += dataPoint.OrderTotal;
                year.Quantity[dataPoint.CreatedOn.Month - 1]++;
            }
        }
    }
}
