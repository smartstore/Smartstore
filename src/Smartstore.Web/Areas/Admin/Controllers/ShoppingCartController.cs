using System.Data;
using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Cart;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class ShoppingCartController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizedEntityService _localizedEntityService;

        public ShoppingCartController(
            SmartDbContext db,
            ICustomerService customerService,
            IShoppingCartService shoppingCartService,
            ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _customerService = customerService;
            _shoppingCartService = shoppingCartService;
            _localizedEntityService = localizedEntityService;
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

        #region Shopping cart settings

        [Permission(Permissions.Configuration.Setting.Read)]
        [LoadSetting]
        public async Task<IActionResult> ShoppingCartSettings(int storeScope, ShoppingCartSettings settings)
        {
            var model = await MapperFactory.MapAsync<ShoppingCartSettings, ShoppingCartSettingsModel>(settings);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.ThirdPartyEmailHandOverLabel = settings.GetLocalizedSetting(x => x.ThirdPartyEmailHandOverLabel, languageId, storeScope, false, false);
            });

            ViewBag.Checkouts = new List<ExtendedSelectListItem>
            {
                CreateCheckoutProcessItem(CheckoutProcess.Standard),
                CreateCheckoutProcessItem(CheckoutProcess.Terminal),
                CreateCheckoutProcessItem(CheckoutProcess.TerminalWithPayment)
            };

            return View(model);

            ExtendedSelectListItem CreateCheckoutProcessItem(string process)
            {
                var item = new ExtendedSelectListItem
                {
                    Text = T("Checkout.Process." + process),
                    Value = process,
                    Selected = settings.CheckoutProcess.EqualsNoCase(process)
                };

                item.CustomProperties["Description"] = T($"Checkout.Process.{process}.Hint").Value;
                return item;
            }
        }

        [Permission(Permissions.Configuration.Setting.Update)]
        [HttpPost, SaveSetting]
        public async Task<IActionResult> ShoppingCartSettings(ShoppingCartSettings settings, ShoppingCartSettingsModel model, int storeScope)
        {
            if (!ModelState.IsValid)
            {
                return await ShoppingCartSettings(storeScope, settings);
            }

            ModelState.Clear();

            await MapperFactory.MapAsync(model, settings);

            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedSettingAsync(settings, 
                    x => x.ThirdPartyEmailHandOverLabel, 
                    localized.ThirdPartyEmailHandOverLabel,
                    localized.LanguageId,
                    storeScope);
            }

            NotifySuccess(T("Admin.Configuration.Updated"));
            return RedirectToAction(nameof(ShoppingCartSettings));
        }

        #endregion
    }
}
