using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Store;
using Smartstore.Admin.Models.Stores;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class StoreController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICatalogSearchService _catalogSearchService;

        public StoreController(SmartDbContext db, ICatalogSearchService catalogSearchService)
        {
            _db = db;
            _catalogSearchService = catalogSearchService;
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
            var storeModels = await Services.StoreContext.GetAllStores()
                .AsQueryable()
                .ApplyGridCommand(command)
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<Store, StoreModel>(x);

                    model.HostList = model.Hosts.Convert<string[]>();
                    model.ViewUrl = Url.Action("Edit", "Store", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var stores = await storeModels
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var gridModel = new GridModel<StoreModel>
            {
                Rows = storeModels,
                Total = stores.TotalCount
            };

            return Json(gridModel);
        }

        [Permission(Permissions.Configuration.Store.Create)]
        public IActionResult Create()
        {
            return View(new StoreModel());
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Store.Create)]
        public async Task<IActionResult> Create(StoreModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var store = await MapperFactory.MapAsync<StoreModel, Store>(model);

                // Ensure we have "/" at the end.
                store.Url = store.Url.EnsureEndsWith("/");
                _db.Stores.Add(store);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Stores.Added"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = store.Id }) : RedirectToAction(nameof(List));
            }

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

                // Ensure we have "/" at the end.
                store.Url = store.Url.EnsureEndsWith("/");
                await _db.SaveChangesAsync();
                
                NotifySuccess(T("Admin.Configuration.Stores.Updated"));
                return continueEditing ? RedirectToAction(nameof(Edit), new { id = store.Id }) : RedirectToAction(nameof(List));
            }

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
                
                // When we delete a store we should also ensure that all "per store" settings will also be deleted.
                await _db.Settings.Where(x => x.StoreId == id).BatchDeleteAsync();

                // When we had two stores and now have only one store, we also should delete all "per store" settings.
                var allStores = await _db.Stores.ToListAsync();
                if (allStores.Count == 1)
                {
                    await _db.Settings.Where(x => x.StoreId == allStores[0].Id).BatchDeleteAsync();
                }

                NotifySuccess(T("Admin.Configuration.Stores.Deleted"));
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = store.Id });
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

            var list = from m in stores
                select new ChoiceListItem
                {
                    Id = m.Id.ToString(),
                    Text = m.Name,
                    Selected = ids.Contains(m.Id)
                };

            return new JsonResult(list.ToList());
        }

        [SaveChanges(typeof(SmartDbContext), false)]
        [Permission(Permissions.Configuration.Store.ReadStats, false)]
        public async Task<JsonResult> StoreDashboardReportAsync()
        {
            var ordersQuery = _db.Orders.AsNoTracking();
            var registeredRole = await _db.CustomerRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);

            var registeredCustomersQuery = _db.Customers
                .AsNoTracking()
                .ApplyRolesFilter(new[] { registeredRole.Id });

            var sumAllOrders = await ordersQuery.SumAsync(x => (decimal?)x.OrderTotal) ?? 0;
            var sumOpenCarts = await _db.ShoppingCartItems.GetOpenCartTypeSubTotalAsync(ShoppingCartType.ShoppingCart);
            var sumWishlists = await _db.ShoppingCartItems.GetOpenCartTypeSubTotalAsync(ShoppingCartType.Wishlist);

            var model = new StoreDashboardReportModel
            {
                ProductsCount = (await _catalogSearchService.PrepareQuery(new CatalogSearchQuery()).CountAsync()).ToString("N0"),
                CategoriesCount = (await _db.Categories.CountAsync()).ToString("N0"),
                ManufacturersCount = (await _db.Manufacturers.CountAsync()).ToString("N0"),
                AttributesCount = (await _db.ProductAttributes.CountAsync()).ToString("N0"),
                AttributeCombinationsCount = (await _db.ProductVariantAttributeCombinations.CountAsync(x => x.IsActive)).ToString("N0"),
                MediaCount = (await Services.MediaService.CountFilesAsync(new MediaSearchQuery { Deleted = false })).ToString("N0"),
                CustomersCount = (await registeredCustomersQuery.CountAsync()).ToString("N0"),
                OrdersCount = (await ordersQuery.CountAsync()).ToString("N0"),
                Sales = Services.CurrencyService.PrimaryCurrency.AsMoney(sumAllOrders).ToString(),
                OnlineCustomersCount = (await _db.Customers.ApplyOnlineCustomersFilter(15).CountAsync()).ToString("N0"),
                CartsValue = Services.CurrencyService.PrimaryCurrency.AsMoney(sumOpenCarts).ToString(),
                WishlistsValue = Services.CurrencyService.PrimaryCurrency.AsMoney(sumWishlists).ToString()
            };

            return new JsonResult(new { model });
        }
    }
}
