
using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Components
{
    public class HomeBestSellersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly CatalogHelper _catalogHelper;
        private readonly IAclService _aclService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStoreMappingService _storeMappingService;
        private readonly CatalogSettings _catalogSettings;

        public HomeBestSellersViewComponent(
            SmartDbContext db,
            CatalogHelper catalogHelper,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IEventPublisher eventPublisher,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _catalogHelper = catalogHelper;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _eventPublisher = eventPublisher;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize = null)
        {
            if (!_catalogSettings.ShowBestsellersOnHomepage || _catalogSettings.NumberOfBestsellersOnHomepage == 0)
            {
                return Empty();
            }

            var bestsellersEvent = new ViewComponentExecutingEvent<List<BestsellersReportLine>>(ViewComponentContext);
            await _eventPublisher.PublishAsync(bestsellersEvent);

            var storeId = Services.StoreContext.CurrentStore.Id;

            // Load report from cache
            var report = bestsellersEvent.Model ?? await Services.Cache.GetAsync(ModelCacheInvalidator.HOMEPAGE_BESTSELLERS_REPORT_KEY.FormatInvariant(storeId), async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(1));

                var query = _db.OrderItems
                    .AsNoTracking()
                    .ApplyOrderFilter(storeId)
                    .ApplyProductFilter()
                    .SelectAsBestsellersReportLine()
                    // INFO: some products may be excluded by ACL or store mapping later, so take more.
                    .Take(Convert.ToInt32(_catalogSettings.NumberOfBestsellersOnHomepage * 1.5));

                return await query.ToListAsync();
            });

            if (report.Count == 0)
            {
                return Empty();
            }

            // Load products
            var products = await _db.Products.GetManyAsync(report.Select(x => x.ProductId));

            // ACL and store mapping
            products = await products
                .WhereAwait(async c => (await _aclService.AuthorizeAsync(c)) && (await _storeMappingService.AuthorizeAsync(c)))
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
