using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Components
{
    public class HomeBestSellersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly CatalogHelper _catalogHelper;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly CatalogSettings _catalogSettings;

        public HomeBestSellersViewComponent(
            SmartDbContext db, 
            CatalogHelper catalogHelper,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _catalogHelper = catalogHelper;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize = null)
        {
            if (!_catalogSettings.ShowBestsellersOnHomepage || _catalogSettings.NumberOfBestsellersOnHomepage == 0)
            {
                return Empty();
            }

            var storeId = Services.StoreContext.CurrentStore.Id;

            // Load report from cache
            var report = await Services.Cache.GetAsync(ModelCacheInvalidator.HOMEPAGE_BESTSELLERS_REPORT_KEY.FormatInvariant(storeId), async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(1));

                // TODO: (ms) (core) Publish event for BestSellers resolution so that modules can jump in with custom resolvers.

                var query = _db.OrderItems
                    .AsNoTracking()
                    .ApplyOrderFilter(storeId)
                    .ApplyProductFilter()
                    .SelectAsBestSellersReportLine()
                    // INFO: some products may be excluded by ACL or store mapping later, so take more.
                    .Take(Convert.ToInt32(_catalogSettings.NumberOfBestsellersOnHomepage * 1.5));

                var sql = query.ToQueryString();

                var bestSellers = await query.ToListAsync();
                return bestSellers;
            });

            if (report.Count == 0)
            {
                return Empty();
            }

            // Load products
            var products = await _db.Products.GetManyAsync(report.Select(x => x.ProductId));

            // ACL and store mapping
            products = await products
                .WhereAsync(async c => (await _aclService.AuthorizeAsync(c)) && (await _storeMappingService.AuthorizeAsync(c)))
                .Take(_catalogSettings.NumberOfBestsellersOnHomepage)
                .AsyncToList();

            var viewMode = _catalogSettings.UseSmallProductBoxOnHomePage ? ProductSummaryViewMode.Mini : ProductSummaryViewMode.Grid;

            var settings = _catalogHelper.GetBestFitProductSummaryMappingSettings(viewMode, x =>
            {
                x.ThumbnailSize = productThumbPictureSize;
            });

            var model = await _catalogHelper.MapProductSummaryModelAsync(products, settings);
            model.GridColumnSpan = GridColumnSpan.Max6Cols;

            return View(model);
        }
    }
}
