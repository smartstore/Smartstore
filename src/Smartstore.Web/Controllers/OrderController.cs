using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Security;

namespace Smartstore.Web.Controllers
{
    public class OrderController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly OrderHelper _orderHelper;

        public OrderController(SmartDbContext db, OrderHelper orderHelper)
        {
            _db = db;
            _orderHelper = orderHelper;
        }

        [RequireSsl]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(x => x.ShippingAddress)
                .Include(x => x.BillingAddress)
                .FindByIdAsync(id, false);

            if (await IsNonExistentOrderAsync(order))
                return NotFound();

            if (await IsUnauthorizedOrderAsync(order))
                return new UnauthorizedResult();

            var model = await _orderHelper.PrepareOrderDetailsModelAsync(order);
            return View(model);
        }

        private async Task<bool> IsNonExistentOrderAsync(Order order)
        {
            var result = order == null || order.Deleted;

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                result = result || (order.StoreId != 0 && order.StoreId != Services.StoreContext.CurrentStore.Id);
            }

            return result;
        }

        private async Task<bool> IsUnauthorizedOrderAsync(Order order)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
                return order == null || order.CustomerId != Services.WorkContext.CurrentCustomer.Id;
            else
                return order == null;
        }
    }
}
