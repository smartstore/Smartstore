using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardRegisteredCustomersViewComponent : DashboardViewComponentBase
    {
        private readonly SmartDbContext _db;

        public DashboardRegisteredCustomersViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public override async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Customer.Read))
            {
                return Empty();
            }

            var registeredRole = await _db.CustomerRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);

            var customerDates = _db.Customers
                .AsNoTracking()
                .ApplyRegistrationFilter(CreatedFrom, Now)
                .ApplyRolesFilter(new[] { registeredRole.Id })
                .Select(x => x.CreatedOnUtc)
                .ToList();

            var model = DashboardChartReportModel.Create(1);

            // Sort data for chart display.
            foreach (var dataPoint in customerDates)
            {
                SetCustomerReportData(model, Services.DateTimeHelper.ConvertToUserTime(dataPoint, DateTimeKind.Utc));
            }

            for (int i = 0; i < model.Count; i++)
            {
                var m = model[i];

                // Format and sum values.
                foreach (var data in m.DataSets)
                {
                    for (int j = 0; j < data.Amount.Length; j++)
                    {
                        data.QuantityFormatted[j] = data.Quantity[j].ToString("N0");
                    }
                    data.TotalAmount = data.Quantity.Sum();
                    data.TotalAmountFormatted = data.TotalAmount.ToString("N0");
                }

                m.TotalAmount = m.DataSets.Sum(x => x.TotalAmount);
                m.TotalAmountFormatted = m.TotalAmount.ToString("N0");

                // Create labels for data points.
                for (int j = 0; j < m.Labels.Length; j++)
                {
                    // Today & yesterday.
                    if (i <= 1)
                    {
                        m.Labels[j] = $"{UserTime.Date.AddHours(j):t} - {UserTime.Date.AddHours(j).AddMinutes(59):t}";
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
                        m.Labels[j] = $"{UserTime.Date.AddDays(fromDay + 7 * j):m} - {UserTime.Date.AddDays(fromDay + 7 * (j + 1) - toDayOffset):m}";
                    }
                    // This year.
                    else if (i == 4)
                    {
                        m.Labels[j] = new DateTime(UserTime.Date.Year, j + 1, 1).ToString("Y");
                    }
                }

                // Get registrations for corresponding period to calculate change in percentage.
                // TODO: only apply to similar time of day?
                if (i == 1 || i == 2)
                {
                    // Day before yesterday or week before.
                    m.ComparedTotalAmount = customerDates.Where(x => x >= m.ComparedFrom && x < m.ComparedTo).Count();
                    ApplyComparedToDescription(m);
                }
                else if (i == 3 || i == 4)
                {
                    // Month or year before.
                    m.ComparedTotalAmount = await _db.Customers
                        .ApplyRegistrationFilter(m.ComparedFrom, m.ComparedTo)
                        .ApplyRolesFilter(new[] { registeredRole.Id })
                        .CountAsync();
                    ApplyComparedToDescription(m);
                }
            }

            // Yesterday.
            model[0].ComparedTotalAmount = model[1].TotalAmount;
            ApplyComparedToDescription(model[0]);

            return View(model);
        }

        private void SetCustomerReportData(List<DashboardChartReportModel> reports, DateTime dataPoint)
        {
            if (dataPoint >= UserTime.Date)
            {
                // Today.
                reports[0].DataSets[0].Quantity[dataPoint.Hour]++;
            }
            else if (dataPoint >= UserTime.AddDays(-1).Date)
            {
                var yesterday = reports[1].DataSets[0];
                yesterday.Quantity[dataPoint.Hour]++;
            }

            if (dataPoint >= UserTime.AddDays(-6).Date)
            {
                // Last 7 days.
                var week = reports[2].DataSets[0];
                var weekIndex = (UserTime.Date - dataPoint.Date).Days;
                week.Quantity[week.Quantity.Length - weekIndex - 1]++;
            }

            if (dataPoint >= UserTime.AddDays(-27).Date)
            {
                // Last 28 days.
                var month = reports[3].DataSets[0];
                var monthIndex = (UserTime.Date - dataPoint.Date).Days / 7;
                month.Quantity[month.Quantity.Length - monthIndex - 1]++;
            }

            if (dataPoint.Year == UserTime.Year)
            {
                // This year.
                reports[4].DataSets[0].Quantity[dataPoint.Month - 1]++;
            }
        }
    }
}
