using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Components
{
    public class PopularProductTagsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IProductTagService _productTagService;
        private readonly CatalogSettings _catalogSettings;

        public PopularProductTagsViewComponent(SmartDbContext db, IProductTagService productTagService, CatalogSettings catalogSettings)
        {
            _db = db;
            _productTagService = productTagService;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_catalogSettings.ShowPopularProductTagsOnHomepage)
            {
                return Empty();
            }

            var store = Services.StoreContext.CurrentStore;
            var cacheKey = string.Format(ModelCacheInvalidator.PRODUCTTAG_POPULAR_MODEL_KEY, Services.WorkContext.WorkingLanguage.Id, store.Id);

            var cacheModel = await Services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(1));

                var productCountsMap = await _productTagService.GetProductCountsMapAsync(null, store.Id);

                var tagIds = productCountsMap
                    .Where(x => x.Value > 0)
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Key)
                    .Take(_catalogSettings.NumberOfProductTags)
                    .ToArray();

                var tags = await _db.ProductTags.GetManyAsync(tagIds);

                var tagModels = tags
                    .Select(tag => new ProductTagModel
                    {
                        Id = tag.Id,
                        Name = tag.GetLocalized(x => x.Name),
                        Slug = tag.BuildSlug(),
                        ProductCount = productCountsMap.Get(tag.Id)
                    })
                    .ToList();

                var model = new PopularProductTagsModel
                {
                    Tags = tagModels.OrderBy(x => x.Name).ToList(),
                    TotalTags = productCountsMap.Count(x => x.Value > 0)
                };

                return model;
            });

            return View(cacheModel);
        }
    }
}
