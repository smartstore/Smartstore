using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Search
{
    public partial class CatalogSearchResult
    {
        private readonly DbSet<Product> _dbSet;
        private IPagedList<Product> _hits;

        /// <summary>
        /// Constructor to get an instance without any search hits.
        /// </summary>
        /// <param name="query">Catalog search query</param>
        public CatalogSearchResult(CatalogSearchQuery query)
            : this(null, query, null, 0, null, null, null)
        {
        }

        public CatalogSearchResult(
            ISearchEngine engine,
            CatalogSearchQuery query,
            DbSet<Product> dbSet,
            int totalHitsCount,
            int[] hitsEntityIds,
            string[] spellCheckerSuggestions,
            IDictionary<string, FacetGroup> facets)
        {
            Guard.NotNull(query, nameof(query));

            Engine = engine;
            Query = query;
            _dbSet = dbSet;
            TotalHitsCount = totalHitsCount;
            HitsEntityIds = hitsEntityIds ?? Array.Empty<int>();
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
        /// Gets the product hits. Once loaded, the result is cached so that
        /// subsequent calls to this method do not hit the database again.
        /// </summary>
        public async Task<IPagedList<Product>> GetHitsAsync()
        {
            if (_hits == null)
            {
                var products = TotalHitsCount > 0 && _dbSet != null && Query.HitsFactory != null
                    ? await Query.HitsFactory.Invoke(_dbSet, HitsEntityIds)
                    : Enumerable.Empty<Product>();

                _hits = products.ToPagedList(Query.PageIndex, Query.Take, TotalHitsCount);
            }

            return _hits;
        }
    }
}
