using Smartstore.Admin.Models.Customers;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components;

public class DashboardTopCustomersViewComponent : SmartViewComponent
{
    /// <summary>
    /// Gets the widget permission required to display this report.
    /// </summary>
    public const string RequiredPermission = Permissions.Dashboard.Widgets.Customers.TopCustomers;

    private readonly SmartDbContext _db;

    public DashboardTopCustomersViewComponent(SmartDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int count = 7)
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
        {
            return Empty();
        }

        var customer = Services.WorkContext.CurrentCustomer;
        var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync("Customer", customer.Id);

        var orderQuery = _db.Orders.Where(x => !x.Customer.Deleted).ApplyCustomerFilter(authorizedStoreIds);

        var reportByQuantity = await orderQuery
            .SelectAsTopCustomerReportLine(ReportSorting.ByQuantityDesc)
            .Take(Math.Clamp(count, 1, 50))
            .ToListAsync();

        var reportByAmount = await orderQuery
            .SelectAsTopCustomerReportLine(ReportSorting.ByAmountDesc)
            .Take(Math.Clamp(count, 1, 50))
            .ToListAsync();

        var model = new DashboardTopCustomersModel
        {
            TopCustomersByQuantity = await reportByQuantity.MapAsync(_db),
            TopCustomersByAmount = await reportByAmount.MapAsync(_db)
        };

        return View(model);
    }
}