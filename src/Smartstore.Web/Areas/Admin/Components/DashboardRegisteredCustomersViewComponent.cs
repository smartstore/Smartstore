using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardRegisteredCustomersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;

        public DashboardRegisteredCustomersViewComponent(SmartDbContext db, IDateTimeHelper dateTimeHelper)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Customer.Read))
            {
                return Empty();
            }

            // Get customers of at least last 28 days (if year is younger)
            var utcNow = DateTime.UtcNow;
            var beginningOfYear = new DateTime(utcNow.Year, 1, 1);
            var startDate = (utcNow.Date - beginningOfYear).Days < 28 ? utcNow.AddDays(-27).Date : beginningOfYear;

            var registeredRole = await _db.CustomerRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);

            var customerDates = _db.Customers
                .AsNoTracking()
                .ApplyRegistrationFilter(startDate, utcNow)
                .ApplyRolesFilter(new[] { registeredRole.Id })
                .Select(x => x.CreatedOnUtc)
                .ToList();

            var model = new List<DashboardChartReportModel>()
            {
                // Today = index 0
                new DashboardChartReportModel(1, 24),
                // Yesterday = index 1
                new DashboardChartReportModel(1, 24),
                // Last 7 days = index 2
                new DashboardChartReportModel(1, 7),
                // Last 28 days = index 3
                new DashboardChartReportModel(1, 4),
                // This year = index 4
                new DashboardChartReportModel(1, 12)
            };

            // Sort data for chart display.
            foreach (var dataPoint in customerDates)
            {
                SetCustomerReportData(model, _dateTimeHelper.ConvertToUserTime(dataPoint, DateTimeKind.Utc));
            }

            var userTime = _dateTimeHelper.ConvertToUserTime(utcNow, DateTimeKind.Utc).Date;
            // Format and sum values, create labels for all dataPoints
            for (int i = 0; i < model.Count; i++)
            {
                foreach (var data in model[i].DataSets)
                {
                    for (int j = 0; j < data.Amount.Length; j++)
                    {
                        data.QuantityFormatted[j] = data.Quantity[j].ToString("N0");
                    }
                    data.TotalAmount = data.Quantity.Sum();
                    data.TotalAmountFormatted = data.TotalAmount.ToString("N0");
                }

                model[i].TotalAmount = model[i].DataSets.Sum(x => x.TotalAmount);
                model[i].TotalAmountFormatted = model[i].TotalAmount.ToString("N0");

                for (int j = 0; j < model[i].Labels.Length; j++)
                {
                    // Today & yesterday
                    if (i <= 1)
                    {
                        model[i].Labels[j] = $"{userTime.AddHours(j):t} - {userTime.AddHours(j).AddMinutes(59):t}";
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
                        model[i].Labels[j] = $"{userTime.AddDays(fromDay + 7 * j):m} - {userTime.AddDays(fromDay + 7 * (j + 1) - toDayOffset):m}";
                    }
                    // This year
                    else if (i == 4)
                    {
                        model[i].Labels[j] = new DateTime(userTime.Year, j + 1, 1).ToString("Y");
                    }
                }
            }

            // Get registrations for corresponding period to calculate change in percentage; TODO: only apply to similar time of day?
            var registeredCountMonthBefore = await _db.Customers
                .ApplyRegistrationFilter(beginningOfYear.AddDays(-56), utcNow.Date.AddDays(-28))
                .ApplyRolesFilter(new[] { registeredRole.Id })
                .CountAsync();

            var registeredCountYearBefore = await _db.Customers
                .ApplyRegistrationFilter(beginningOfYear.AddYears(-1), utcNow.AddYears(-1))
                .ApplyRolesFilter(new[] { registeredRole.Id })
                .CountAsync();

            var sumBefore = new decimal[]
            {
                // Get registration count for day before
                model[1].TotalAmount,

                // Get registration count for day before yesterday
                customerDates.Where( x =>
                    x >= utcNow.Date.AddDays(-2) && x < utcNow.Date.AddDays(-1)
                ).Count(),

                // Get registration count for week before
                customerDates.Where( x =>
                    x >= utcNow.Date.AddDays(-14) && x < utcNow.Date.AddDays(-7)
                ).Count(),

                // Get registration count for month & year before
                registeredCountMonthBefore,
                registeredCountYearBefore
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

        private void SetCustomerReportData(List<DashboardChartReportModel> reports, DateTime dataPoint)
        {
            var userTime = _dateTimeHelper.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Utc);

            // Today
            if (dataPoint >= userTime.Date)
            {
                reports[0].DataSets[0].Quantity[dataPoint.Hour]++;
            }
            // Yesterday
            else if (dataPoint >= userTime.AddDays(-1).Date)
            {
                var yesterday = reports[1].DataSets[0];
                yesterday.Quantity[dataPoint.Hour]++;
            }

            // Within last 7 days
            if (dataPoint >= userTime.AddDays(-6).Date)
            {
                var week = reports[2].DataSets[0];
                var weekIndex = (userTime.Date - dataPoint.Date).Days;
                week.Quantity[week.Quantity.Length - weekIndex - 1]++;
            }

            // Within last 28 days  
            if (dataPoint >= userTime.AddDays(-27).Date)
            {
                var month = reports[3].DataSets[0];
                var monthIndex = (userTime.Date - dataPoint.Date).Days / 7;
                month.Quantity[month.Quantity.Length - monthIndex - 1]++;
            }

            // Within this year
            if (dataPoint.Year == userTime.Year)
            {
                reports[4].DataSets[0].Quantity[dataPoint.Month - 1]++;
            }
        }
    }
}
