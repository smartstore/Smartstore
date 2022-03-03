using Smartstore.Admin.Models.Customers;
using Smartstore.Core.Checkout.Orders.Reporting;

namespace Smartstore.Admin.Components
{
    public class DashboardTopCustomersViewComponent : SmartViewComponent
    {
        private const int NUM_REPORT_LINES = 7;

        private readonly SmartDbContext _db;

        public DashboardTopCustomersViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var orderQuery = _db.Orders.Where(x => !x.Customer.Deleted);

            var reportByQuantity = await orderQuery
                .SelectAsTopCustomerReportLine(ReportSorting.ByQuantityDesc)
                .Take(NUM_REPORT_LINES)
                .ToListAsync();

            var reportByAmount = await orderQuery
                .SelectAsTopCustomerReportLine(ReportSorting.ByAmountDesc)
                .Take(NUM_REPORT_LINES)
                .ToListAsync();

            var model = new DashboardTopCustomersModel
            {
                TopCustomersByQuantity = await reportByQuantity.MapAsync(_db),
                TopCustomersByAmount = await reportByAmount.MapAsync(_db)
            };

            return View(model);
        }
    }
}
