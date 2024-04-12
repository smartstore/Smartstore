using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Store;
using Smartstore.Admin.Models.Stores;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Utilities;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class StoreController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public StoreController(
            SmartDbContext db, 
            ICatalogSearchService catalogSearchService,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _catalogSearchService = catalogSearchService;
            _shoppingCartSettings = shoppingCartSettings;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available stores. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all stores as JSON.</returns>
        public IActionResult AllStores(string label, string selectedIds)
        {
            var stores = new List<Store>(Services.StoreContext.GetAllStores());
            var ids = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                stores.Insert(0, new Store { Name = label, Id = 0 });
            }

            var list = stores
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = ids.Contains(x.Id)
                })
                .ToList();

            return new JsonResult(list);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Store.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> StoreList(GridCommand command)
        {
            var stores = Services.StoreContext.GetAllStores();
            var mapper = MapperFactory.GetMapper<Store, StoreModel>();

            var rows = await stores
                .AsQueryable()
                .ApplyGridCommand(command)
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.HostList = model.Hosts.Convert<string[]>();
                    model.EditUrl = Url.Action(nameof(Edit), "Store", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            return Json(new GridModel<StoreModel>
            {
                Rows = rows,
                Total = rows.Count
            });
        }

        [Permission(Permissions.Configuration.Store.Create)]
        public async Task<IActionResult> Create()
        {
            await PrepareViewBag(null);

            var model = new StoreModel
            {
                DefaultCurrencyId = Services.CurrencyService.PrimaryCurrency.Id
            };

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Store.Create)]
        public async Task<IActionResult> Create(StoreModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var store = await MapperFactory.MapAsync<StoreModel, Store>(model);

                _db.Stores.Add(store);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Stores.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = store.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareViewBag(null);

            return View(model);
        }

        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var store = Services.StoreContext.GetStoreById(id);
            if (store == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = await MapperFactory.MapAsync<Store, StoreModel>(store);

            await PrepareViewBag(store);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Configuration.Store.Update)]
        public async Task<IActionResult> Edit(StoreModel model, bool continueEditing)
        {
            var store = await _db.Stores.FindByIdAsync(model.Id);
            if (store == null)
            {
                return RedirectToAction(nameof(List));
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, store);

                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Stores.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = store.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareViewBag(store);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var store = await _db.Stores.FindByIdAsync(id);
            if (store == null)
            {
                return RedirectToAction(nameof(List));
            }

            try
            {
                _db.Stores.Remove(store);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Stores.Deleted"));
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = store.Id });
        }

        [SaveChanges<SmartDbContext>(false)]
        [Permission(Permissions.Configuration.Store.ReadStats, false)]
        public async Task<JsonResult> StoreDashboardReportAsync()
        {
            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;
            var ordersQuery = _db.Orders.AsNoTracking();
            var registeredRole = await _db.CustomerRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);

            var registeredCustomersQuery = _db.Customers
                .AsNoTracking()
                .ApplyRolesFilter([registeredRole.Id]);

            var sumAllOrders = await ordersQuery.SumAsync(x => (decimal?)x.OrderTotal) ?? 0;
            var sumOpenCarts = await _db.ShoppingCartItems.GetOpenCartTypeSubTotalAsync(ShoppingCartType.ShoppingCart, _shoppingCartSettings.AllowActivatableCartItems ? true : null);
            var sumWishlists = await _db.ShoppingCartItems.GetOpenCartTypeSubTotalAsync(ShoppingCartType.Wishlist);
            var totalMediaSize = await _db.MediaFiles.SumAsync(x => (long)x.Size);

            var model = new StoreDashboardReportModel
            {
                ProductsCount = (await _catalogSearchService.PrepareQuery(new CatalogSearchQuery()).CountAsync()).ToString("N0"),
                CategoriesCount = (await _db.Categories.CountAsync()).ToString("N0"),
                ManufacturersCount = (await _db.Manufacturers.CountAsync()).ToString("N0"),
                AttributesCount = (await _db.ProductAttributes.CountAsync()).ToString("N0"),
                AttributeCombinationsCount = (await _db.ProductVariantAttributeCombinations.CountAsync(x => x.IsActive)).ToString("N0"),
                MediaCount = (await Services.MediaService.CountFilesAsync(new MediaSearchQuery { Deleted = false })).ToString("N0"),
                MediaSize = Prettifier.HumanizeBytes(totalMediaSize),
                CustomersCount = (await registeredCustomersQuery.CountAsync()).ToString("N0"),
                OrdersCount = (await ordersQuery.CountAsync()).ToString("N0"),
                OnlineCustomersCount = (await _db.Customers.ApplyOnlineCustomersFilter(15).CountAsync()).ToString("N0"),
                Sales = Services.CurrencyService.CreateMoney(sumAllOrders, primaryCurrency).ToString(),
                CartsValue = Services.CurrencyService.CreateMoney(sumOpenCarts, primaryCurrency).ToString(),
                WishlistsValue = Services.CurrencyService.CreateMoney(sumWishlists, primaryCurrency).ToString()
            };

            return new JsonResult(new { model });
        }

        private async Task PrepareViewBag(Store store)
        {
            var currencies = await _db.Currencies
                .AsNoTracking()
                .ApplyStandardFilter(false, store?.Id ?? 0)
                .ToListAsync();

            ViewBag.Currencies = currencies
                .Select(x => new SelectListItem
                {
                    Text = x.GetLocalized(y => y.Name),
                    Value = x.Id.ToString()
                })
                .ToList();
        }
    }
}