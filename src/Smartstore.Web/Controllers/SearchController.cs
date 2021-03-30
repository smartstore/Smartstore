using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Search;

namespace Smartstore.Web.Controllers
{
    public class SearchController : PublicControllerBase
    {
        private readonly CatalogHelper _catalogHelper;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;
        
        public SearchController(
            CatalogHelper catalogHelper,
            ICatalogSearchService catalogSearchService,
            ILocalizedEntityService localizedEntityService,
            MediaSettings mediaSettings,
            SearchSettings searchSettings)
        {
            _catalogHelper = catalogHelper;
            _catalogSearchService = catalogSearchService;
            _localizedEntityService = localizedEntityService;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
        }

        [HttpPost]
        [LocalizedRoute("/instantsearch", Name = "InstantSearch")]
        public async Task<IActionResult> InstantSearch(CatalogSearchQuery query)
        {
            if (!query.Term.HasValue() || query.Term.Length < _searchSettings.InstantSearchTermMinLength)
            {
                return Content(string.Empty);
            }

            query
                .BuildFacetMap(false)
                .Slice(0, Math.Min(16, _searchSettings.InstantSearchNumberOfProducts))
                .SortBy(ProductSortingEnum.Relevance);

            var result = await _catalogSearchService.SearchAsync(query);

            var model = new SearchResultModel(query)
            {
                SearchResult = result,
                Term = query.Term,
                TotalProductsCount = result.TotalHitsCount
            };

            var mappingSettings = _catalogHelper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Mini, x =>
            {
                x.MapPrices = false;
                x.MapShortDescription = true;
                x.MapPictures = _searchSettings.ShowProductImagesInInstantSearch;
                x.ThumbnailSize = _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage;
                x.PrefetchTranslations = true;
                x.PrefetchUrlSlugs = true;
            });

            // TODO: (mh) (core) What about scoped services?
            //using (_localizedEntityService.BeginScope(false))
            //{
                var hits = await result.GetHitsAsync();
                // InstantSearch should be REALLY very fast! No time for smart caching stuff.
                if (result.TotalHitsCount > 0)
                {
                    await _localizedEntityService.PrefetchLocalizedPropertiesAsync(
                        nameof(Product),
                        Services.WorkContext.WorkingLanguage.Id,
                        hits.Select(x => x.Id).ToArray());
                }

                // Add product hits.
                model.TopProducts = await _catalogHelper.MapProductSummaryModelAsync(hits, mappingSettings);

                // Add spell checker suggestions (if any).
                model.AddSpellCheckerSuggestions(result.SpellCheckerSuggestions, T, x => Url.RouteUrl("Search", new { q = x }));
            //}

            return PartialView(model);
        }
    }
}
