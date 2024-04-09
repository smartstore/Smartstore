using System.Data;
using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Cart;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class ShoppingCartController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;

        public ShoppingCartController(
            SmartDbContext db,
            ICustomerService customerService,
            IShoppingCartService shoppingCartService)
        {
            _db = db;
            _customerService = customerService;
            _shoppingCartService = shoppingCartService;
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

            ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();

            return View(model);
        }

        [Permission(Permissions.Cart.Read)]
        public IActionResult CurrentWishlists()
        {
            var model = new CurrentCartListModel
            {
                CartType = ShoppingCartType.Wishlist
            };

            ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();

            return View(model);
        }

        [Permission(Permissions.Cart.Read)]
        public async Task<IActionResult> CurrentCartList(GridCommand command, CurrentCartListModel model)
        {
            var dtHelper = Services.DateTimeHelper;
            var cartTypeId = (int)model.CartType;
            var guestStr = T("Admin.Customers.Guest").Value;
            var guestRole = await _customerService.GetRoleBySystemNameAsync(SystemCustomerRoleNames.Guests, false);
            var guestRoleId = guestRole?.Id ?? 0;

            DateTime? startDateUtc = model.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDateUtc = model.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            // INFO: the first where-clause is important for performance (avoid aggregating across all customers).
            var query =
                from c in _db.Customers
                let cartItems = c.ShoppingCartItems.Where(x =>
                    x.ShoppingCartTypeId == cartTypeId && x.ParentItemId == null
                    && (startDateUtc == null || startDateUtc.Value <= x.CreatedOnUtc)
                    && (endDateUtc == null || endDateUtc.Value >= x.CreatedOnUtc)
                    && (model.StoreId == 0 || x.StoreId == model.StoreId))
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

        [Permission(Permissions.Cart.Read)]
        public async Task<IActionResult> CurrentCartDetailsList(int customerId, ShoppingCartType cartType)
        {
            var customer = await _db.Customers
                .IncludeShoppingCart()
                .FindByIdAsync(customerId);

            if (customer == null)
            {
                return NotFound();
            }

            var cart = await _shoppingCartService.GetCartAsync(customer, cartType, 0, null);
            var rows = await cart.MapAsync();

            return Json(new GridModel<ShoppingCartItemModel>
            {
                Rows = rows,
                Total = rows.Count
            });
        }
    }
}
