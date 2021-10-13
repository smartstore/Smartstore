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
        private readonly CustomerHelper _customerHelper;

        public DashboardTopCustomersViewComponent(SmartDbContext db, CustomerHelper customerHelper)
        {
            _db = db;
            _customerHelper = customerHelper;
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
                TopCustomersByQuantity = await _customerHelper.CreateCustomerReportLineModelAsync(reportByQuantity),
                TopCustomersByAmount = await _customerHelper.CreateCustomerReportLineModelAsync(reportByAmount)
            };

            return View(model);
        }
    }
}
