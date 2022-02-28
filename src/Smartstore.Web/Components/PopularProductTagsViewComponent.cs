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

                var customer = Services.WorkContext.CurrentCustomer;
                var model = new PopularProductTagsModel();

                // TODO: (mg) (core) This is gonna explode with large amount of tags. Rethink!
                var allTags = await _db.ProductTags
                    .Where(x => x.Published)
                    .ToListAsync();

                var tags = (from t in allTags
                          let numProducts = _productTagService.CountProductsByTagIdAsync(t.Id, customer, store.Id).Await()
                          where numProducts > 0
                          orderby numProducts descending
                          select new 
                          {
                              Tag = t,
                              LocalizedName = t.GetLocalized(x => x.Name),
                              NumProducts = numProducts
                          }).ToList();

                tags = tags
                    .OrderBy(x => x.LocalizedName.Value)
                    .Take(_catalogSettings.NumberOfProductTags)
                    .ToList();

                model.TotalTags = allTags.Count;

                foreach (var tag in tags)
                {
                    model.Tags.Add(new ProductTagModel
                    {
                        Id = tag.Tag.Id,
                        Name = tag.LocalizedName,
                        Slug = tag.Tag.BuildSlug(),
                        ProductCount = tag.NumProducts
                    });
                }

                return model;
            });

            return View(cacheModel);
        }
    }
}
