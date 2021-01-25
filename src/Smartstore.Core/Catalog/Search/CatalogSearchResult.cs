using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Search
{
    public partial class CatalogSearchResult
    {
        private readonly Func<Task<IList<Product>>> _hitsFactory;
        private IPagedList<Product> _hits;

        /// <summary>
        /// Constructor to get an instance without any search hits.
        /// </summary>
        /// <param name="query">Catalog search query</param>
        public CatalogSearchResult(CatalogSearchQuery query)
            : this(null, query, 0, null, null, null, null)
        {
        }

        public CatalogSearchResult(
            ISearchEngine engine,
            CatalogSearchQuery query,
            int totalHitsCount,
            int[] hitsEntityIds,
            Func<Task<IList<Product>>> hitsFactory,
            string[] spellCheckerSuggestions,
            IDictionary<string, FacetGroup> facets)
        {
            Guard.NotNull(query, nameof(query));

            Engine = engine;
            Query = query;
            TotalHitsCount = totalHitsCount;
            HitsEntityIds = hitsEntityIds ?? Array.Empty<int>();
            _hitsFactory = hitsFactory;
            //_hitsFactory = hitsFactory ?? (() => Task.FromResult<IList<Product>>(new List<Product>()));
            SpellCheckerSuggestions = spellCheckerSuggestions ?? Array.Empty<string>();
            Facets = facets ?? new Dictionary<string, FacetGroup>();
        }

        /// <summary>
        /// The original catalog search query.
        /// </summary>
        public CatalogSearchQuery Query { get; init; }

        /// <summary>
        /// Entity identifiers of found products.
        /// </summary>
        public int[] HitsEntityIds { get; init; }

        public int TotalHitsCount { get; init; }

        /// <summary>
        /// Gets spell checking suggestions/corrections.
        /// </summary>
        public string[] SpellCheckerSuggestions { get; set; }

        public IDictionary<string, FacetGroup> Facets { get; init; }

        public ISearchEngine Engine { get; init; }

        /// <summary>
        /// Gets product hits.
        /// </summary>
        public async Task<IPagedList<Product>> GetHitsAsync()
        {
            if (_hits == null)
            {
                var products = TotalHitsCount > 0 && _hitsFactory != null
                    ? await _hitsFactory.Invoke()
                    : Enumerable.Empty<Product>();

                _hits = new PagedList<Product>(products, Query.PageIndex, Query.Take, TotalHitsCount);
            }

            return _hits;
        }
    }
}
