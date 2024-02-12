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
        private readonly Lazy<IProductService> _productService;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly MediaSettings _mediaSettings;
        private readonly SearchSettings _searchSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly SeoSettings _seoSettings;

        public SearchController(
            CatalogHelper catalogHelper,
            ICatalogSearchService catalogSearchService,
            ILocalizedEntityService localizedEntityService,
            Lazy<IProductService> productService,
            ProductUrlHelper productUrlHelper,
            MediaSettings mediaSettings,
            SearchSettings searchSettings,
            CatalogSettings catalogSettings,
            SeoSettings seoSettings)
        {
            _catalogHelper = catalogHelper;
            _catalogSearchService = catalogSearchService;
            _localizedEntityService = localizedEntityService;
            _productService = productService;
            _productUrlHelper = productUrlHelper;
            _mediaSettings = mediaSettings;
            _searchSettings = searchSettings;
            _catalogSettings = catalogSettings;
            _seoSettings = seoSettings;
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
            CatalogSearchResult result = null;
            var model = new SearchResultModel(query);
            var term = query?.DefaultTerm;

            if ((term == null || term.Length < _searchSettings.InstantSearchTermMinLength) && !query.HasSelectedFacets)
            {
                model.SearchResult = new CatalogSearchResult(query);
                model.TopProducts = ProductSummaryModel.Empty;
                model.Error = T("Search.SearchTermMinimumLengthIsNCharacters", _searchSettings.InstantSearchTermMinLength);
                return View(model);
            }

            var customer = Services.WorkContext.CurrentCustomer;
            if (!customer.IsSystemAccount)
            {
                customer.GenericAttributes.LastContinueShoppingPage = HttpContext.Request.RawUrl();
            }

            try
            {
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

                result = await _catalogSearchService.SearchAsync(query);
            }
            catch (Exception ex)
            {
                model.Error = ex.ToString();
                result = new CatalogSearchResult(query);
            }

            if (result.TotalHitsCount == 0 && result.SpellCheckerSuggestions.Length > 0)
            {
                // No matches, but spell checker made a suggestion.
                // We implicitly search again with the first suggested term.
                var oldSuggestions = result.SpellCheckerSuggestions;

                query.DefaultTerm = oldSuggestions[0];
                result = await _catalogSearchService.SearchAsync(query);

                if (result.TotalHitsCount > 0)
                {
                    model.AttemptedTerm = term;
                    // Restore the original suggestions.
                    result.SpellCheckerSuggestions = oldSuggestions.Where(x => x != query.DefaultTerm).ToArray();
                }
                else
                {
                    query.DefaultTerm = term;
                }
            }

            model.SearchResult = result;
            model.Term = query.DefaultTerm;
            model.TotalProductsCount = result.TotalHitsCount;

            var productSummaryViewMode = query.CustomData.Get("ViewMode") is string viewMode && viewMode.EqualsNoCase("list")
                ? ProductSummaryViewMode.List
                : ProductSummaryViewMode.Grid;

            var mappingSettings = _catalogHelper.GetBestFitProductSummaryMappingSettings(productSummaryViewMode);
            var summaryModel = await _catalogHelper.MapProductSummaryModelAsync(result, mappingSettings);

            // Prepare paging/sorting/mode stuff.
            _catalogHelper.MapListActions(summaryModel, null, _catalogSettings.DefaultPageSizeOptions);

            // Add product hits.
            model.TopProducts = summaryModel;

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                var page = query.PageIndex + 1;
                object queryValues = page == 1
                    ? new { q = query.DefaultTerm }
                    : new { q = query.DefaultTerm, i = page };

                model.CanonicalUrl = Url.RouteUrl("Search", queryValues, Request.Scheme);
            }

            return View(model);
        }
    }
}
