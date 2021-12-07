using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Forums.Search;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Search;

namespace Smartstore.Forums.Models.Public
{
    public partial class ForumSearchResultModel : SearchResultModelBase, IForumSearchResultModel, IListActions
    {
        public ForumSearchResultModel(ForumSearchQuery query)
        {
            Query = query;
        }

        public ForumSearchQuery Query { get; }
        public ForumSearchResult SearchResult { get; set; }

        /// <summary>
        /// Contains the original/misspelled search term, when the search did not match any results 
        /// and the spell checker suggested at least one term.
        /// </summary>
        public string AttemptedTerm { get; set; }
        public string Term { get; set; }

        public int CumulativeHitCount { get; set; }
        public int TotalCount { get; set; }
        public string Error { get; set; }

        public override List<HitGroup> HitGroups { get; protected set; } = new();

        public bool AllowFiltering => true;
        public bool AllowSorting { get; set; }
        public int? CurrentSortOrder { get; set; }
        public string CurrentSortOrderName { get; set; }
        public string RelevanceSortOrderName { get; set; }
        public Dictionary<int, string> AvailableSortOptions { get; set; }
        public IPageable PagedList { get; set; }

        public ProductSummaryViewMode ViewMode => throw new NotImplementedException();
        public GridColumnSpan GridColumnSpan => throw new NotImplementedException();
        public bool AllowViewModeChanging => throw new NotImplementedException();
        public IEnumerable<int> AvailablePageSizes => throw new NotImplementedException();
    }
}
