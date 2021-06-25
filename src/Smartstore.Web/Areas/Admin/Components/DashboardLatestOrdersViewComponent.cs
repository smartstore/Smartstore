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
            var latestOrders = await _db.Orders.ApplyPaging(0, 7).ToListAsync();
            
            foreach (var order in latestOrders)
            {
                model.LatestOrders.Add(
                    new DashboardOrderModel(
                        order.CustomerId,
                        order.Customer.FindEmail() ?? order.Customer.FormatUserName(),
                        order.OrderItems.Sum(x => x.Quantity),
                        Services.WorkContext.WorkingCurrency.AsMoney(order.OrderTotal).ToString(),
                        _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToString("g"),
                        order.OrderStatus,
                        order.Id)
                    );
            }

            return View(model);
        }
    }
}
