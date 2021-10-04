using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Core.Search;
using Smartstore.Core.Search.Facets;
using Smartstore.Diagnostics;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Search
{
    public partial class ForumSearchService : SearchServiceBase, IForumSearchService
    {
        private readonly ICommonServices _services;
        private readonly IIndexManager _indexManager;

        public ForumSearchService(
            ICommonServices services,
            IIndexManager indexManager)
        {
            _services = services;
            _indexManager = indexManager;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public static string Scope => "Forum";

        public IQueryable<ForumPost> PrepareQuery(ForumSearchQuery searchQuery, IQueryable<ForumPost> baseQuery = null)
        {
            var linqForumSearchService = _services.Resolve<LinqForumSearchService>();
            return linqForumSearchService.PrepareQuery(searchQuery, baseQuery);
        }

        public async Task<ForumSearchResult> SearchAsync(ForumSearchQuery searchQuery, bool direct = false)
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

                    var totalCount = 0;
                    int[] hitsEntityIds = null;
                    string[] spellCheckerSuggestions = null;
                    IEnumerable<ISearchHit> searchHits;
                    IDictionary<string, FacetGroup> facets = null;

                    await _services.EventPublisher.PublishAsync(new ForumSearchingEvent(searchQuery));

                    if (searchQuery.Take > 0)
                    {
                        using (_services.Chronometer.Step(stepPrefix + "Search"))
                        {
                            totalCount = await searchEngine.CountAsync();
                            // Fix paging boundaries.
                            if (searchQuery.Skip > 0 && searchQuery.Skip >= totalCount)
                            {
                                searchQuery.Slice((totalCount / searchQuery.Take) * searchQuery.Take, searchQuery.Take);
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

                    var result = new ForumSearchResult(
                        searchEngine,
                        searchQuery,
                        _services.DbContext.ForumPosts(),
                        totalCount,
                        hitsEntityIds,
                        spellCheckerSuggestions,
                        facets);

                    await _services.EventPublisher.PublishAsync(new ForumSearchedEvent(searchQuery, result));

                    return result;
                }
                else if (searchQuery.Origin.EqualsNoCase("Boards/Search"))
                {
                    IndexingRequiredNotification(_services);
                }
            }

            return await SearchDirectAsync(searchQuery);
        }

        protected virtual async Task<ForumSearchResult> SearchDirectAsync(ForumSearchQuery searchQuery)
        {
            var linqForumSearchService = _services.Resolve<LinqForumSearchService>();
            var result = await linqForumSearchService.SearchAsync(searchQuery, true);

            return result;
        }
    }
}
