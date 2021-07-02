using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Store;
using Smartstore.Admin.Models.Stores;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data.Batching;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class StoreController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly ICatalogSearchService _catalogSearchService;

        public StoreController(SmartDbContext db, ICatalogSearchService catalogSearchService)
        {
            _db = db;
            _catalogSearchService = catalogSearchService;
        }

        private async Task PrepareStoreModelAsync(StoreModel model, Store store)
        {
            var currencies = await _db.Currencies
                .AsNoTracking()
                .ApplyStandardFilter(false, store == null ? 0 : store.Id)
                .ToListAsync();

            model.AvailableCurrencies = currencies
                .Select(x => new SelectListItem
                {
                    Text = x.GetLocalized(y => y.Name),
                    Value = x.Id.ToString()
                })
                .ToList();
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Configuration.Store.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> StoreList(GridCommand command, StoreModel model)
        {
            var storeModels = await Services.StoreContext.GetAllStores()
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<Store, StoreModel>(x);

                    await PrepareStoreModelAsync(model, x);

                    model.Hosts = model.Hosts.EmptyNull().Replace(",", "<br />");
                    model.ViewUrl = Url.Action("Edit", "Store", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var stores = await storeModels.ToPagedList(command.Page - 1, command.PageSize).LoadAsync();
            var gridModel = new GridModel<StoreModel>
            {
                Rows = storeModels,
                Total = stores.TotalCount
            };

            return Json(gridModel);
        }

        [Permission(Permissions.Configuration.Store.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new StoreModel();
            await PrepareStoreModelAsync(model, null);

            return View(model);
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
                return continueEditing ? RedirectToAction("Edit", new { id = store.Id }) : RedirectToAction("List");
            }

            await PrepareStoreModelAsync(model, null);
            return View(model);
        }

        [Permission(Permissions.Configuration.Store.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var store = Services.StoreContext.GetStoreById(id);
            if (store == null)
            {
                return RedirectToAction("List");
            }

            var model = await MapperFactory.MapAsync<Store, StoreModel>(store);
            await PrepareStoreModelAsync(model, store);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Configuration.Store.Update)]
        public async Task<IActionResult> Edit(StoreModel model, bool continueEditing)
        {
            var store = Services.StoreContext.GetStoreById(model.Id);
            if (store == null)
            {
                return RedirectToAction("List");
            }

            if (ModelState.IsValid)
            {
                store = await MapperFactory.MapAsync<StoreModel, Store>(model);

                // Ensure we have "/" at the end.
                store.Url = store.Url.EnsureEndsWith("/");
                _db.Stores.Update(store);
                await _db.SaveChangesAsync();
                
                NotifySuccess(T("Admin.Configuration.Stores.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = store.Id }) : RedirectToAction("List");
            }

            await PrepareStoreModelAsync(model, store);
            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var store = Services.StoreContext.GetStoreById(id);
            if (store == null)
            {
                return RedirectToAction("List");
            }

            try
            {
                _db.Stores.Remove(store);
                await _db.SaveChangesAsync();
                
                // When we delete a store we should also ensure that all "per store" settings will also be deleted.
                await _db.Settings.Where(x => x.StoreId == id).BatchDeleteAsync();

                // When we had two stores and now have only one store, we also should delete all "per store" settings.
                var allStores = Services.StoreContext.GetAllStores();
                if (allStores.Count == 1)
                {
                    await _db.Settings.Where(x => x.StoreId == allStores.FirstOrDefault().Id).BatchDeleteAsync();
                }

                NotifySuccess(T("Admin.Configuration.Stores.Deleted"));
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id = store.Id });
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
