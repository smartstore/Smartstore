using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Customers;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Web.Components;

namespace Smartstore.Admin.Components
{
    public class DashboardTopCustomersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly CustomerSettings _customerSettings;

        public DashboardTopCustomersViewComponent(SmartDbContext db, CustomerSettings customerSettings)
        {
            _db = db;
            _customerSettings = customerSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pageSize = 7;

            var reportByQuantity = await _db.Customers
                .SelectAsTopCustomerReportLine(sorting: ReportSorting.ByQuantityDesc)
                .Take(pageSize)
                .ToListAsync();

            var reportByAmount = await _db.Customers
                .SelectAsTopCustomerReportLine(sorting: ReportSorting.ByAmountDesc)
                .Take(pageSize)
                .ToListAsync();

            var model = new DashboardTopCustomersModel
            {
                TopCustomersByQuantity = await CreateCustomerReportLineModelAsync(reportByQuantity),
                TopCustomersByAmount = await CreateCustomerReportLineModelAsync(reportByAmount)
            };

            return View(model);
        }

        private async Task<List<TopCustomerReportLineModel>> CreateCustomerReportLineModelAsync(IList<TopCustomerReportLine> items)
        {
            var customerIds = items.Distinct().Select(x => x.CustomerId).ToArray();
            var customers = await _db.Customers
                .AsNoTracking()
                .Include(x => x.BillingAddress)
                .Include(x => x.ShippingAddress)
                .Where(x => customerIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var guestStr = T("Admin.Customers.Guest").Value;

            var model = items.Select(x =>
            {
                customers.TryGetValue(x.CustomerId, out var customer);

                var m = new TopCustomerReportLineModel
                {
                    OrderTotal = Services.CurrencyService.PrimaryCurrency.AsMoney(x.OrderTotal),
                    OrderCount = x.OrderCount.ToString("N0"),
                    CustomerId = x.CustomerId,
                    CustomerNumber = customer?.CustomerNumber,
                    CustomerDisplayName = customer?.FindEmail() ?? customer?.FormatUserName(_customerSettings, T, false) ?? string.Empty.NaIfEmpty(),
                    Email = customer?.Email.NullEmpty() ?? (customer.IsGuest() ? guestStr : string.Empty.NaIfEmpty()),
                    Username = customer?.Username,
                    FullName = customer?.GetFullName(),
                    Active = customer?.Active ?? false,
                    LastActivityDate = Services.DateTimeHelper.ConvertToUserTime(customer?.LastActivityDateUtc ?? DateTime.MinValue, DateTimeKind.Utc)
                };

                return m;
            })
            .ToList();

            return model;
        }
    }
}
