using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Search.Indexing;
using Smartstore.Core.Seo;
using Smartstore.Diagnostics;

namespace Smartstore.Core.Catalog.Search
{
    public partial class CatalogSearchService : SearchServiceBase, ICatalogSearchService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IIndexManager _indexManager;
        private readonly ICurrencyService _currencyService;

        public CatalogSearchService(
            SmartDbContext db,
            ICommonServices services,
            IIndexManager indexManager,
            ICurrencyService currencyService)
        {
            _db = db;
            _services = services;
            _indexManager = indexManager;
            _currencyService = currencyService;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public static string Scope => "Catalog";

        public IQueryable<Product> PrepareQuery(CatalogSearchQuery searchQuery, IQueryable<Product> baseQuery = null)
        {
            var linqCatalogSearchService = _services.ResolveNamed<ICatalogSearchService>("linq");
            return linqCatalogSearchService.PrepareQuery(searchQuery, baseQuery);
        }

        public async Task<CatalogSearchResult> SearchAsync(CatalogSearchQuery searchQuery, bool direct = false)
        {
            Guard.NotNull(searchQuery, nameof(searchQuery));
            Guard.NotNegative(searchQuery.Take, nameof(searchQuery.Take));

            var provider = _indexManager.GetIndexProvider(Scope);

            if (!direct && provider != null)
            {
                var indexStore = provider.GetIndexStore(Scope);
                if (indexStore.Exists)
                {
                    var searchEngine = provider.GetSearchEngine(indexStore, searchQuery);
                    var stepPrefix = searchEngine.GetType().Name + " - ";

                    int totalCount = 0;
                    int[] hitsEntityIds = null;
                    string[] spellCheckerSuggestions = null;
                    IEnumerable<ISearchHit> searchHits;
                    IDictionary<string, FacetGroup> facets = null;

                    await _services.EventPublisher.PublishAsync(new CatalogSearchingEvent(searchQuery, false));

                    if (searchQuery.Take > 0)
                    {
                        using (_services.Chronometer.Step(stepPrefix + "Search"))
                        {
                            totalCount = await searchEngine.CountAsync();
                            // Fix paging boundaries.
                            if (searchQuery.Skip > 0 && searchQuery.Skip > totalCount)
                            {
                                searchQuery.Slice(totalCount, searchQuery.Take);
                            }
                        }

                        if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithHits))
                        {
                            using (_services.Chronometer.Step(stepPrefix + "Hits"))
                            {
                                searchHits = await searchEngine.SearchAsync();
                            }

                            hitsEntityIds = searchHits.Select(x => x.EntityId).ToArray();
                        }

                        if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithFacets))
                        {
                            try
                            {
                                using (_services.Chronometer.Step(stepPrefix + "Facets"))
                                {
                                    facets = await searchEngine.GetFacetMapAsync();
                                    ApplyFacetLabels(facets);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                        }
                    }

                    if (searchQuery.ResultFlags.HasFlag(SearchResultFlags.WithSuggestions))
                    {
                        try
                        {
                            using (_services.Chronometer.Step(stepPrefix + "Spellcheck"))
                            {
                                spellCheckerSuggestions = await searchEngine.CheckSpellingAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Spell checking should not break the search.
                            Logger.Error(ex);
                        }
                    }

                    var result = new CatalogSearchResult(
                        searchEngine,
                        searchQuery,
                        _db.Products,
                        totalCount,
                        hitsEntityIds,
                        spellCheckerSuggestions,
                        facets);

                    var searchedEvent = new CatalogSearchedEvent(searchQuery, result);
                    await _services.EventPublisher.PublishAsync(searchedEvent);

                    return searchedEvent.Result;
                }
                else if (searchQuery.IsSearchPage())
                {
                    IndexingRequiredNotification(_services);
                }
            }

            return await SearchDirectAsync(searchQuery);
        }

        /// <summary>
        /// Bypasses the index provider and directly searches in the database.
        /// </summary>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Catalog search result.</returns>
        protected virtual async Task<CatalogSearchResult> SearchDirectAsync(CatalogSearchQuery searchQuery)
        {
            var linqCatalogSearchService = _services.ResolveNamed<ICatalogSearchService>("linq");
            var result = await linqCatalogSearchService.SearchAsync(searchQuery, true);
            ApplyFacetLabels(result.Facets);

            return result;
        }

        protected virtual void ApplyFacetLabels(IDictionary<string, FacetGroup> facets)
        {
            if (!(facets?.Any() ?? false))
            {
                return;
            }

            var rangeMinTemplate = T("Search.Facet.RangeMin").Value;
            var rangeMaxTemplate = T("Search.Facet.RangeMax").Value;
            var rangeBetweenTemplate = T("Search.Facet.RangeBetween").Value;

            // Apply "price" labels.
            if (facets.TryGetValue("price", out var group))
            {
                // TODO: formatting without decimals would be nice.
                foreach (var facet in group.Facets)
                {
                    var val = facet.Value;

                    if (val.Value == null && val.UpperValue != null)
                    {
                        val.Label = rangeMaxTemplate.FormatInvariant(FormatPrice(val.UpperValue.Convert<decimal>()));
                    }
                    else if (val.Value != null && val.UpperValue == null)
                    {
                        val.Label = rangeMinTemplate.FormatInvariant(FormatPrice(val.Value.Convert<decimal>()));
                    }
                    else if (val.Value != null && val.UpperValue != null)
                    {
                        val.Label = rangeBetweenTemplate.FormatInvariant(
                            FormatPrice(val.Value.Convert<decimal>()),
                            FormatPrice(val.UpperValue.Convert<decimal>()));
                    }
                }
            }

            // Apply "rating" labels.
            if (facets.TryGetValue("rating", out group))
            {
                foreach (var facet in group.Facets)
                {
                    facet.Value.Label = T(facet.Key == "1" ? "Search.Facet.1StarAndMore" : "Search.Facet.XStarsAndMore", facet.Value.Value).Value;
                }
            }

            // Apply "numeric range" labels.
            var numericRanges = facets
                .Where(x => x.Value.TemplateHint == FacetTemplateHint.NumericRange)
                .Select(x => x.Value);

            foreach (var numericRange in numericRanges)
            {
                foreach (var facet in numericRange.SelectedFacets)
                {
                    var val = facet.Value;
                    var labels = val.Label.SplitSafe('~');

                    if (val.Value == null && val.UpperValue != null)
                    {
                        val.Label = rangeMaxTemplate.FormatInvariant(labels.FirstOrDefault());
                    }
                    else if (val.Value != null && val.UpperValue == null)
                    {
                        val.Label = rangeMinTemplate.FormatInvariant(labels.FirstOrDefault());
                    }
                    else if (val.Value != null && val.UpperValue != null)
                    {
                        val.Label = rangeBetweenTemplate.FormatInvariant(labels.FirstOrDefault(), labels.Skip(1).FirstOrDefault());
                    }
                }
            }
        }

        protected virtual string FormatPrice(decimal price)
        {
            return _currencyService.ConvertToWorkingCurrency(price).ToString();
        }

        #region XML Sitemap

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesProducts)
            {
                return null;
            }

            var searchQuery = new CatalogSearchQuery()
                .VisibleOnly(_services.WorkContext.CurrentCustomer)
                .WithVisibility(ProductVisibility.Full)
                .HasStoreId(context.RequestStoreId);

            var query = PrepareQuery(searchQuery);

            return new ProductXmlSitemapResult { Query = query, Context = context };
        }

        class ProductXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<Product> Query { get; set; }
            public XmlSitemapBuildContext Context { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var entityName = nameof(Product);
                var pager = Query.ToFastPager(Context.MaximumNodeCount);

                while ((await pager.ReadNextPageAsync(x => new { x.Id, x.UpdatedOnUtc }, x => x.Id)).Out(out var products))
                {
                    if (Context.CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    foreach (var product in products)
                    {
                        yield return new NamedEntity { EntityName = entityName, Id = product.Id, LastMod = product.UpdatedOnUtc };
                    }
                }
            }

            public override int Order => int.MaxValue;
        }

        #endregion
    }
}
