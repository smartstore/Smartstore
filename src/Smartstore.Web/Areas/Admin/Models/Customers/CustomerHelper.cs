using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Customers
{
    public partial class CustomerHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly CustomerSettings _customerSettings;
        private readonly IUrlHelper _urlHelper;
        
        public CustomerHelper(SmartDbContext db, ICommonServices services, CustomerSettings customerSettings, IUrlHelper urlHelper)
        {
            _db = db;
            _services = services;
            _customerSettings = customerSettings;
            _urlHelper = urlHelper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<List<TopCustomerReportLineModel>> CreateCustomerReportLineModelAsync(IList<TopCustomerReportLine> items)
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
                    OrderTotal = _services.CurrencyService.PrimaryCurrency.AsMoney(x.OrderTotal),
                    OrderCount = x.OrderCount.ToString("N0"),
                    CustomerId = x.CustomerId,
                    CustomerNumber = customer?.CustomerNumber,
                    CustomerDisplayName = customer?.FindEmail() ?? customer?.FormatUserName(_customerSettings, T, false) ?? string.Empty.NaIfEmpty(),
                    Email = customer?.Email.NullEmpty() ?? (customer.IsGuest() ? guestStr : string.Empty.NaIfEmpty()),
                    Username = customer?.Username,
                    FullName = customer?.GetFullName(),
                    Active = customer?.Active ?? false,
                    LastActivityDate = _services.DateTimeHelper.ConvertToUserTime(customer?.LastActivityDateUtc ?? DateTime.MinValue, DateTimeKind.Utc),
                    EditUrl = _urlHelper.Action("Edit", "Customer", new { id = customer.Id })
                };

                return m;
            })
            .ToList();

            return model;
        }
    }
}
