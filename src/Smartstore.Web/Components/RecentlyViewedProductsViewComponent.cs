using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Components
{
    public class RecentlyViewedProductsViewComponent : SmartViewComponent
    {
        private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly CatalogHelper _catalogHelper;
        private readonly CatalogSettings _catalogSettings;

        public RecentlyViewedProductsViewComponent(
            IRecentlyViewedProductsService recentlyViewedProductsService, 
            CatalogHelper catalogHelper,
            CatalogSettings catalogSettings)
        {
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _catalogHelper = catalogHelper;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled)
            {
                return Empty();
            }

            var products = await _recentlyViewedProductsService.GetRecentlyViewedProductsAsync(_catalogSettings.RecentlyViewedProductsNumber);
            if (products.Count == 0)
            {
                return Empty();
            }

            var settings = _catalogHelper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Mini, x =>
            {
                x.MapManufacturers = _catalogSettings.ShowManufacturerInGridStyleLists;
            });

            var model = await _catalogHelper.MapProductSummaryModelAsync(products, settings);

            return View(model);
        }
    }
}
