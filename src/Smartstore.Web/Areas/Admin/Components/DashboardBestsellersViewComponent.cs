using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardBestsellersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public DashboardBestsellersViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                return Empty();
            }

            const int pageSize = 7;

            // INFO: join tables to ignore soft-deleted products and orders.
            var orderItemQuery =
                from oi in _db.OrderItems.AsNoTracking()
                join o in _db.Orders.AsNoTracking() on oi.OrderId equals o.Id
                join p in _db.Products.AsNoTracking() on oi.ProductId equals p.Id
                where !p.IsSystemProduct
                select oi;

            var reportByQuantity = await orderItemQuery
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByQuantityDesc)
                .Take(pageSize)
                .ToListAsync();

            var reportByAmount = await orderItemQuery
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByAmountDesc)
                .Take(pageSize)
                .ToListAsync();

            var model = new DashboardBestsellersModel
            {
                BestsellersByQuantity = await reportByQuantity.MapAsync(Services),
                BestsellersByAmount = await reportByAmount.MapAsync(Services)
            };

            return View(model);
        }
    }
}
