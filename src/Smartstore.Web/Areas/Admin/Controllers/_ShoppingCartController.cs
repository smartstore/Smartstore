using System;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.ShoppingCart;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    // TODO: (mg) (core) remove ShoppingCartItemQueryExtensions.CountCartItemsAsync when ready (not required, looks weird anyway).
    public class ShoppingCartController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICustomerService _customerService;

        public ShoppingCartController(SmartDbContext db, ICustomerService customerService)
        {
            _db = db;
            _customerService = customerService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(CurrentCarts));
        }

        [Permission(Permissions.Cart.Read)]
        public IActionResult CurrentCarts()
        {
            var model = new CurrentCartListModel
            {
                CartType = ShoppingCartType.ShoppingCart
            };

            return View(model);
        }

        [Permission(Permissions.Cart.Read)]
        public async Task<IActionResult> CurrentCartsList(GridCommand command, CurrentCartListModel model)
        {
            var cartTypeId = (int)ShoppingCartType.ShoppingCart;
            var guestStr = T("Admin.Customers.Guest").Value;
            var guestRole = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Guests, false);
            var guestRoleId = guestRole?.Id ?? 0;

            // INFO: the first where-clause is important for performance (avoid aggregating across all customers).
            var query =
                from c in _db.Customers
                let cartItems = c.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == cartTypeId && x.ParentItemId == null)
                where cartItems.Any()
                select new CurrentCartModel
                {
                    CustomerId = c.Id,
                    CustomerEmail = c.Email,
                    IsGuest = c.CustomerRoleMappings.Any(x => x.CustomerRoleId == guestRoleId),
                    TotalItems = cartItems.Sum(x => x.Quantity),
                    LatestCartItemDate = cartItems
                        .Select(x => (DateTime?)x.CreatedOnUtc)
                        .OrderByDescending(x => x)
                        .FirstOrDefault()
                };

            var rows = await query
                .OrderByDescending(x => x.LatestCartItemDate)
                .ToPagedList(command)
                .LoadAsync();

            foreach (var row in rows)
            {
                if (row.CustomerEmail.IsEmpty())
                {
                    row.CustomerEmail = row.IsGuest ? guestStr : StringExtensions.NotAvailable;
                }

                if (row.LatestCartItemDate.HasValue)
                {
                    row.LatestCartItemDate = Services.DateTimeHelper.ConvertToUserTime(row.LatestCartItemDate.Value, DateTimeKind.Utc);
                }

                row.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = row.CustomerId });
            }

            return Json(new GridModel<CurrentCartModel>
            {
                Rows = rows,
                Total = rows.TotalCount
            });
        }


    }
}
