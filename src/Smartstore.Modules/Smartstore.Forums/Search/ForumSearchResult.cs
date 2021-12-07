using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Forums.Search
{
    public interface IForumSearchResultModel
    {
        ForumSearchResult SearchResult { get; }
    }

    public partial class ForumSearchResult
    {
        private readonly DbSet<ForumPost> _dbSet;
        private IPagedList<ForumPost> _hits;

        public ForumSearchResult(
            ISearchEngine engine,
            ForumSearchQuery query,
            DbSet<ForumPost> dbSet,
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
            SpellCheckerSuggestions = spellCheckerSuggestions ?? Array.Empty<string>();
            Facets = facets ?? new Dictionary<string, FacetGroup>();
        }

        /// <summary>
        /// Constructor to get an instance without any search hits.
        /// </summary>
        /// <param name="query">Forum search query.</param>
        public ForumSearchResult(ForumSearchQuery query)
            : this(null, query, null, 0, null, null, null)
        {
        }

        /// <summary>
        /// Entity identifiers of found forum posts.
        /// </summary>
        public int[] HitsEntityIds { get; }

        public int TotalHitsCount { get; }

        /// <summary>
        /// The original forum search query.
        /// </summary>
        public ForumSearchQuery Query { get; }

        /// <summary>
        /// Gets spell checking suggestions/corrections.
        /// </summary>
        public string[] SpellCheckerSuggestions { get; set; }

        public IDictionary<string, FacetGroup> Facets { get; }

        public ISearchEngine Engine { get; }

        /// <summary>
        /// Gets the forum posts hits. Once loaded, the result is cached so that
        /// subsequent calls to this method do not hit the database again.
        /// </summary>
        public async Task<IPagedList<ForumPost>> GetHitsAsync()
        {
            if (_hits == null)
            {
                var products = TotalHitsCount > 0 && _dbSet != null && Query.GetHitsFactory() != null
                    ? await Query.GetHitsFactory().Invoke(_dbSet, HitsEntityIds)
                    : Enumerable.Empty<ForumPost>();

                _hits = products.ToPagedList(Query.PageIndex, Query.Take, TotalHitsCount);
            }

            return _hits;
        }
    }
}
