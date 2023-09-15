using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Search;

namespace Smartstore.Web.Controllers
{
    public class SearchController : PublicController
    {
        private readonly CatalogHelper _catalogHelper;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly Lazy<IProductService> _productService;
        private readonly ProductUrlHelper _productUrlHelper;
        //private SearchResultModel _searchResultModel;

        public SearchController(
            CatalogHelper catalogHelper,
            ICatalogSearchService catalogSearchService,
            ILocalizedEntityService localizedEntityService,
            MediaSettings mediaSettings,
            SearchSettings searchSettings,
            CatalogSettings catalogSettings,
            Lazy<IProductService> productService,
            ProductUrlHelper productUrlHelper
            //SearchResultModel search
            )
        {
            _catalogHelper = catalogHelper;
            _catalogSearchService = catalogSearchService;
            _localizedEntityService = localizedEntityService;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
            _catalogSettings = catalogSettings;
            _productService = productService;
            _productUrlHelper = productUrlHelper;

            //if(search == null)
            //{
            //}
            //else
            //{
            //    _searchResultModel = search;
            //}
            //_searchResultModel = new SearchResultModel();
        }

        [HttpPost]
        [LocalizedRoute("/instantsearch", Name = "InstantSearch")]
        public async Task<IActionResult> InstantSearch(CatalogSearchQuery query)
        {
            var term = query?.DefaultTerm;

            if (!term.HasValue() || term.Length < _searchSettings.InstantSearchTermMinLength)
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
                Term = term,
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
            model.TopProducts = await _catalogHelper.MapProductSummaryModelAsync(hits, result, mappingSettings);

            // Add spell checker suggestions (if any).
            model.AddSpellCheckerSuggestions(result.SpellCheckerSuggestions, T, x => Url.RouteUrl("Search", new { q = x }));

            return PartialView(model);
        }

        [LocalizedRoute("/search", Name = "Search")]
        public async Task<IActionResult> Search(CatalogSearchQuery query)
        {
            var term = query?.DefaultTerm;

            if (_searchSettings.SearchProductByIdentificationNumber)
            {
                var (product, attributeCombination) = await _productService.Value.GetProductByCodeAsync(term);
                if (product != null)
                {
                        if (attributeCombination != null)
                        {
                            return Redirect(await _productUrlHelper.GetProductPathAsync(
                                product.Id,
                                await product.GetActiveSlugAsync(),
                                attributeCombination.AttributeSelection));
                        }

                        return RedirectToRoute("Product", new { SeName = await product.GetActiveSlugAsync() });

                    
                }
            }
            return View(await GetSearchResultModel(query));
        }

        public async Task<SearchResultModel> GetSearchResultModel(CatalogSearchQuery query)
        {
            CatalogSearchResult result = null;
            var model = new SearchResultModel(query);


            //break into it's own method search settings
            
            var service = new SearchControllerService(_catalogSearchService, _searchSettings, _catalogSettings, _catalogHelper);
            return await service.GetSearchResultService(model, result, query);
        }
    }
}
