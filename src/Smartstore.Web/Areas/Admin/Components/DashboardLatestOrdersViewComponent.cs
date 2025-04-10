using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Admin.Components
{
    public class DashboardLatestOrdersViewComponent(
        CustomerSettings customerSettings) : SmartViewComponent
    {
        private readonly CustomerSettings _customerSettings = customerSettings;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                return Empty();
            }

            var model = new DashboardLatestOrdersModel();
            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;
            var authorizedStoreIds = await Services.StoreMappingService.GetAuthorizedStoreIdsAsync("Customer", Services.WorkContext.CurrentCustomer.Id);
            var latestOrders = await Services.DbContext.Orders
                .ApplyCustomerFilter(authorizedStoreIds)
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.Customer)
                    .ThenInclude(x => x.CustomerRoleMappings)
                    .ThenInclude(x => x.CustomerRole)
                .Include(x => x.OrderItems)
                .OrderByDescending(x => x.Id)
                .Take(7)
                .ToListAsync();

            foreach (var order in latestOrders)
            {
                model.LatestOrders.Add(new()
                {
                    OrderNumber = order.OrderNumber.NullEmpty() ?? order.Id.ToString(),
                    CustomerId = order.CustomerId,
                    CustomerDisplayName = order.Customer.FormatUserName(_customerSettings, T, false, true),
                    ProductsTotal = order.OrderItems.Sum(x => x.Quantity),
                    TotalAmount = Services.CurrencyService.CreateMoney(order.OrderTotal, primaryCurrency),
                    Created = Services.DateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("g"),
                    OrderState = order.OrderStatus,
                    OrderId = order.Id
                });
            }

            return View(model);
        }
    }
}
