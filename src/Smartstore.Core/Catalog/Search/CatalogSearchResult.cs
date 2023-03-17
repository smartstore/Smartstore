using Smartstore.Collections;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Catalog.Search
{
    public partial class CatalogSearchResult : Disposable
    {
        private IPagedList<Product> _hits;

        private readonly WeakReference<CatalogSearchQuery> _query;
        private readonly WeakReference<DbSet<Product>> _dbSet;

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
            Guard.NotNull(query);

            _query = new WeakReference<CatalogSearchQuery>(query);
            _dbSet = new WeakReference<DbSet<Product>>(dbSet);

            Engine = engine;
            TotalHitsCount = totalHitsCount;
            HitsEntityIds = hitsEntityIds ?? Array.Empty<int>();
            SpellCheckerSuggestions = spellCheckerSuggestions ?? Array.Empty<string>();
            Facets = facets ?? new Dictionary<string, FacetGroup>();
        }

        private DbSet<Product> DbSet
        {
            get
            {
                _dbSet.TryGetTarget(out var dbSet);
                return dbSet;
            }
        }

        /// <summary>
        /// The original catalog search query.
        /// </summary>
        public CatalogSearchQuery Query 
        { 
            get
            {
                _query.TryGetTarget(out var query);
                return query;
            }
        }

        public ISearchEngine Engine { get; }

        /// <summary>
        /// Entity identifiers of found products.
        /// </summary>
        public int[] HitsEntityIds { get; }

        public int TotalHitsCount { get; }

        /// <summary>
        /// Gets spell checking suggestions/corrections.
        /// </summary>
        public string[] SpellCheckerSuggestions { get; set; }

        public IDictionary<string, FacetGroup> Facets { get; }

        /// <summary>
        /// Gets the product hits. Once loaded, the result is cached so that
        /// subsequent calls to this method do not hit the database again.
        /// </summary>
        public async Task<IPagedList<Product>> GetHitsAsync()
        {
            if (_hits == null)
            {
                var dbSet = DbSet;
                var query = Query;
                var products = TotalHitsCount > 0 && dbSet != null && query.GetHitsFactory() != null
                    ? await query.GetHitsFactory().Invoke(dbSet, HitsEntityIds)
                    : Enumerable.Empty<Product>();

                _hits = products.ToPagedList(query.PageIndex, query.Take, TotalHitsCount);
            }

            return _hits;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _hits?.Clear();
            }
        }
    }
}
