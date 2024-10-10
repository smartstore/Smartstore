using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Store;
using Smartstore.Admin.Models.Stores;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
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
            var customerAuthorizedStores = await Services.StoreMappingService.GetCustomerAuthorizedStoreIdsAsync();
            var mapper = MapperFactory.GetMapper<Store, StoreModel>();

            var rows = await stores
                .Where(store => customerAuthorizedStores.Length != 0 ? customerAuthorizedStores.Any(cas => store.Id == cas) : true)
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
            var authorizedStoreIds = await Services.StoreMappingService.GetCustomerAuthorizedStoreIdsAsync();
            
            var customerStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(Customer), [.. _db.Customers.Select(x => x.Id)]);
            var productStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(Product), [.. _db.Products.Select(x => x.Id)]);
            var categoryStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(Category), [.. _db.Categories.Select(x => x.Id)]);
            var manufacturerStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(Manufacturer), [.. _db.MediaFiles.Select(x => x.Id)]);
            var attributesCountStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(ProductAttribute), [.. _db.ProductAttributes.Select(x => x.Id)]);
            var attributeCombinationsCountStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(ProductVariantAttributeCombination), [.. _db.MediaFiles.Select(x => x.Id)]);
            var shoppingCartItemStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(ShoppingCartItem), [.. _db.ShoppingCartItems.Select(x => x.Id)]);
            var mediaFileStoreMappings = await Services.StoreMappingService.GetStoreMappingCollectionAsync(nameof(MediaFile), [.. _db.MediaFiles.Select(x => x.Id)]);
            var filteredCustomers = _db.Customers.ApplyCustomerStoreFilter(authorizedStoreIds, customerStoreMappings);

            var registeredRole = await _db.CustomerRoles.AsNoTracking().FirstOrDefaultAsync(x => x.SystemName == SystemCustomerRoleNames.Registered);
            var ordersQuery = _db.Orders.ApplyCustomerStoreFilter(authorizedStoreIds).AsNoTracking();
            var sumAllOrders = await ordersQuery.SumAsync(x => (decimal?)x.OrderTotal) ?? 0;
            var shoppingCartItems = _db.ShoppingCartItems.ApplyCustomerStoreFilter(authorizedStoreIds, shoppingCartItemStoreMappings);
            var sumOpenCarts = await shoppingCartItems.GetOpenCartTypeSubTotalAsync(ShoppingCartType.ShoppingCart, _shoppingCartSettings.AllowActivatableCartItems ? true : null);
            var sumWishlists = await shoppingCartItems.GetOpenCartTypeSubTotalAsync(ShoppingCartType.Wishlist);
            var totalMediaSize = await _db.MediaFiles.ApplyCustomerStoreFilter(authorizedStoreIds, mediaFileStoreMappings).SumAsync(x => (long)x.Size);

            var model = new StoreDashboardReportModel
            {
                ProductsCount = (await _catalogSearchService.PrepareQuery(new CatalogSearchQuery()).ApplyCustomerStoreFilter(authorizedStoreIds, productStoreMappings).CountAsync()).ToString("N0"),
                CategoriesCount = (await _db.Categories.ApplyCustomerStoreFilter(authorizedStoreIds, categoryStoreMappings).CountAsync()).ToString("N0"),
                ManufacturersCount = (await _db.Manufacturers.ApplyCustomerStoreFilter(authorizedStoreIds, manufacturerStoreMappings).CountAsync()).ToString("N0"),
                AttributesCount = (await _db.ProductAttributes.ApplyCustomerStoreFilter(authorizedStoreIds, attributesCountStoreMappings).CountAsync()).ToString("N0"),
                AttributeCombinationsCount = (await _db.ProductVariantAttributeCombinations.ApplyCustomerStoreFilter(authorizedStoreIds, attributeCombinationsCountStoreMappings).CountAsync(x => x.IsActive)).ToString("N0"),
                MediaCount = (await Services.MediaService.CountFilesAsync(new MediaSearchQuery { Deleted = false })).ToString("N0"),
                MediaSize = Prettifier.HumanizeBytes(totalMediaSize),
                CustomersCount = (await filteredCustomers.AsNoTracking().ApplyRolesFilter([registeredRole.Id]).CountAsync()).ToString("N0"),
                OnlineCustomersCount = (await filteredCustomers.ApplyOnlineCustomersFilter(15).CountAsync()).ToString("N0"),
                OrdersCount = (await ordersQuery.CountAsync()).ToString("N0"),
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