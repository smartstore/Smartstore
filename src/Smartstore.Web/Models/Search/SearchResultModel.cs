using Smartstore.Core.Catalog.Search;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Models.Search
{
    public class SearchResultModel : SearchResultModelBase, ISearchResultModel
    {
        public SearchResultModel(CatalogSearchQuery query)
        {
            Query = query;
        }

        public CatalogSearchQuery Query { get; }

        public CatalogSearchResult SearchResult { get; set; }

        /// <summary>
        /// Contains the original/misspelled search term, when
        /// the search did not match any results and the spell checker
        /// suggested at least one term.
        /// </summary>
        public string AttemptedTerm { get; set; }

        public string Term { get; set; }

        public ProductSummaryModel TopProducts { get; set; }

        public int TotalProductsCount { get; set; }

        public override List<HitGroup> HitGroups { get; protected set; } = new();

        public string Error { get; set; }
        public string CanonicalUrl { get; set; }
    }
}
