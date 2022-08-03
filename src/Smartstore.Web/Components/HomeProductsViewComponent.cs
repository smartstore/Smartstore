using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Components
{
    public class HomeProductsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly CatalogHelper _catalogHelper;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly CatalogSettings _catalogSettings;

        public HomeProductsViewComponent(
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
            var products = await _db.Products
                .AsNoTracking()
                .ApplyStandardFilter(false)
                .Where(x => x.ShowOnHomePage)
                .OrderBy(x => x.HomePageDisplayOrder)
                .ToListAsync();

            // ACL and store mapping
            products = await products
                .WhereAwait(async c => (await _aclService.AuthorizeAsync(c)) && (await _storeMappingService.AuthorizeAsync(c)))
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
