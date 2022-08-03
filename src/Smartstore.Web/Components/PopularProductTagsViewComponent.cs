using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Data;
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

                var model = new PopularProductTagsModel();
                var productCountsMap = await _productTagService.GetProductCountsMapAsync(null, store.Id);
                var pager = new FastPager<ProductTag>(_db.ProductTags.AsNoTracking().Where(x => x.Published), 1000);

                while (model.Tags.Count < _catalogSettings.NumberOfProductTags && (await pager.ReadNextPageAsync<ProductTag>()).Out(out var tags))
                {
                    foreach (var tag in tags)
                    {
                        if (productCountsMap.TryGetValue(tag.Id, out var productCount) && productCount > 0)
                        {
                            model.Tags.Add(new ProductTagModel
                            {
                                Id = tag.Id,
                                Name = tag.GetLocalized(x => x.Name),
                                Slug = tag.BuildSlug(),
                                ProductCount = productCount
                            });

                            if (model.Tags.Count >= _catalogSettings.NumberOfProductTags)
                            {
                                break;
                            }
                        }
                    }
                }

                model.Tags = model.Tags.OrderBy(x => x.Name).ToList();
                model.TotalTags = productCountsMap.Count(x => x.Value > 0);

                return model;
            });

            return View(cacheModel);
        }
    }
}
