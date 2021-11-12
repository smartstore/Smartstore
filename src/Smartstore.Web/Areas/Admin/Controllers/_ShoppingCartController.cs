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

        public ShoppingCartController(SmartDbContext db)
        {
            _db = db;
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
            var query = _db.Customers
                .AsNoTracking()
                .Include(x => x.ShoppingCartItems)
                .IncludeCustomerRoles()
                .ApplyShoppingCartFilter2(ShoppingCartType.ShoppingCart);

            // TODO: (mg) (core) add filters for grid.

            var customers = await query
                .ToPagedList(command)
                .LoadAsync();

            var guestStr = T("Admin.Customers.Guest").Value;

            var rows = customers.Select(x =>
            {
                $"{x.Id}: {x.ShoppingCartItems.Count}".Dump();
                var m = new CurrentCartModel
                {
                    CustomerId = x.Id,
                    CustomerEmail = x.IsGuest() ? guestStr : x.Email,
                    CustomerEditUrl = Url.Action("Edit", "Customer", new { id = x.Id }),
                    TotalItems = x.ShoppingCartItems
                        .Where(x => x.ParentItemId == null && x.ShoppingCartType == ShoppingCartType.ShoppingCart)
                        .Sum(x => x.Quantity)
                };

                return m;
            })
            .ToList();

            return Json(new GridModel<CurrentCartModel>
            {
                Rows = rows,
                Total = customers.TotalCount
            });
        }


    }
}
