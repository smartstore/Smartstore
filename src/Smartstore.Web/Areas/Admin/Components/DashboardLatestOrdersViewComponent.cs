using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardLatestOrdersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IDateTimeHelper _dateTimeHelper;

        public DashboardLatestOrdersViewComponent(SmartDbContext db, IDateTimeHelper dateTimeHelper)
        {
            _db = db;
            _dateTimeHelper = dateTimeHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                return Empty();
            }

            var model = new DashboardLatestOrdersModel();
            var latestOrders = await _db.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Customer)
                    .ThenInclude(x => x.CustomerRoleMappings)
                    .ThenInclude(x => x.CustomerRole)
                .Include(x => x.OrderItems)
                .OrderByDescending(x => x.CreatedOnUtc)
                .Take(7)
                .ToListAsync();

            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;

            foreach (var order in latestOrders)
            {
                model.LatestOrders.Add(
                    new DashboardOrderModel
                    {
                        CustomerId = order.CustomerId,
                        CustomerDisplayName = order.Customer.FindEmail() ?? order.Customer.FormatUserName(),
                        ProductsTotal = order.OrderItems.Sum(x => x.Quantity),
                        TotalAmount = Services.CurrencyService.CreateMoney(order.OrderTotal, primaryCurrency),
                        Created = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("g"),
                        OrderState = order.OrderStatus,
                        OrderId = order.Id
                    });
            }

            return View(model);
        }
    }
}
