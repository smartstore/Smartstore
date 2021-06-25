using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Web.Components;

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
            var model = new DashboardLatestOrdersModel();
            var latestOrders = await _db.Orders
                .AsNoTracking()
                .Include(x => x.Customer).ThenInclude(x => x.CustomerRoleMappings).ThenInclude(x => x.CustomerRole)
                .Include(x => x.OrderItems)
                .OrderByDescending(x => x.CreatedOnUtc) // INFO: (mh) (core) !!!!!! Please analyze THOROUGHLY!!
                .Take(7)
                .ToListAsync();
            
            foreach (var order in latestOrders)
            {
                model.LatestOrders.Add(
                    new DashboardOrderModel
                    {
                        CustomerId = order.CustomerId,
                        CustomerDisplayName = order.Customer.FindEmail() ?? order.Customer.FormatUserName(),
                        ProductsTotal = order.OrderItems.Sum(x => x.Quantity),
                        TotalAmount = Services.CurrencyService.PrimaryCurrency.AsMoney(order.OrderTotal).ToString(),
                        Created = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("g"),
                        OrderState = order.OrderStatus,
                        OrderId = order.Id
                    });
            }

            return View(model);
        }
    }
}
