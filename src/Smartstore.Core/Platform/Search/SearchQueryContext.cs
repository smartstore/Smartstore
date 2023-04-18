namespace Smartstore.Core.Search
{
    public class SearchQueryContext<TQuery> where TQuery : ISearchQuery
    {
        public SearchQueryContext(TQuery query)
        {
            SearchQuery = Guard.NotNull(query);

            CopyFilters(query.Filters);
        }

        public TQuery SearchQuery { get; }
        
        public DateTime Now { get; init; } = DateTime.UtcNow;
        public bool IsGroupingRequired { get; set; }

        /// <summary>
        /// All query filters.
        /// </summary>
        public List<ISearchFilter> Filters { get; } = new();

        protected virtual void CopyFilters(ICollection<ISearchFilter> filters)
        {
            Filters.AddRange(filters);
        }
    }
}
