using Smartstore.Caching;
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

    public async Task<IViewComponentResult> InvokeAsync(int count = 7, bool refresh = false, TimeSpan? cacheDuration = null)
    {
        if (!await Services.Permissions.AuthorizeAsync(RequiredPermission))
        {
            return Empty();
        }

        var customer = Services.WorkContext.CurrentCustomer;
        var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync("Customer", customer.Id);

        count = Math.Clamp(count, 1, 50);

        var storeKey = authorizedStoreIds.Length == 0 ? "all" : string.Join("-", authorizedStoreIds.Order());
        var cache = cacheDuration.HasValue ? Services.Cache : NullCache.Instance;
        var cacheKey = $"admin:dashboard:topcustomers:{storeKey}:{count}";
        if (refresh)
        {
            await cache.RemoveAsync(cacheKey);
        }

        var reports = await cache.GetAsync(cacheKey, async ctx =>
        {
            if (cacheDuration.HasValue)
            {
                ctx.ExpiresIn(cacheDuration.Value);
            }

            var orderQuery = _db.Orders.Where(x => !x.Customer.Deleted).ApplyCustomerFilter(authorizedStoreIds);

            var reportByQuantity = await orderQuery
                .SelectAsTopCustomerReportLine(ReportSorting.ByQuantityDesc)
                .Take(count)
                .ToListAsync();

            var reportByAmount = await orderQuery
                .SelectAsTopCustomerReportLine(ReportSorting.ByAmountDesc)
                .Take(count)
                .ToListAsync();

            return new[] { reportByQuantity, reportByAmount };
        });

        var model = new DashboardTopCustomersModel
        {
            TopCustomersByQuantity = await reports[0].MapAsync(_db),
            TopCustomersByAmount = await reports[1].MapAsync(_db)
        };

        return View(model);
    }
}