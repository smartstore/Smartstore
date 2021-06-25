using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Store;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

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

        public IActionResult Index()
        {
            return RedirectToAction("List");
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
            // INFO: (mh) (core) ???????????? Really?????!!!!! WTF!!!
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
