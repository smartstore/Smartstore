using Smartstore.Caching;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components;

public class DashboardBestsellersViewComponent : SmartViewComponent
{
    /// <summary>
    /// Gets the widget permission required to display this report.
    /// </summary>
    public const string RequiredPermission = Permissions.Dashboard.Widgets.Sales.Bestsellers;

    private readonly SmartDbContext _db;

    public DashboardBestsellersViewComponent(SmartDbContext db)
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
        var cacheKey = $"admin:dashboard:bestsellers:{storeKey}:{count}";
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

            // INFO: join tables to ignore soft-deleted products and orders.
            var orderItemQuery =
                from oi in _db.OrderItems.AsNoTracking()
                join o in _db.Orders.ApplyCustomerFilter(authorizedStoreIds).AsNoTracking() on oi.OrderId equals o.Id
                join p in _db.Products.AsNoTracking() on oi.ProductId equals p.Id
                where !p.IsSystemProduct
                select oi;

            var reportByQuantity = await orderItemQuery
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByQuantityDesc)
                .Take(count)
                .ToListAsync();

            var reportByAmount = await orderItemQuery
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByAmountDesc)
                .Take(count)
                .ToListAsync();

            return new[] { reportByQuantity, reportByAmount };
        });

        var model = new DashboardBestsellersModel
        {
            BestsellersByQuantity = await reports[0].MapAsync(Services),
            BestsellersByAmount = await reports[1].MapAsync(Services)
        };

        return View(model);
    }
}