namespace Smartstore.Core.Search
{
    public class SearchQueryContext<TQuery> where TQuery : ISearchQuery
    {
        public SearchQueryContext(TQuery query)
        {
            SearchQuery = Guard.NotNull(query);

            FlattenFilters(query.Filters);
        }

        public TQuery SearchQuery { get; }
        
        public DateTime Now { get; init; } = DateTime.UtcNow;
        public bool IsGroupingRequired { get; set; }

        /// <summary>
        /// All filters flattened.
        /// </summary>
        public List<ISearchFilter> Filters { get; } = new();

        /// <summary>
        /// Attribute filters except range filters.
        /// </summary>
        public List<IAttributeSearchFilter> AttributeFilters { get; } = new();

        /// <summary>
        /// Range filters only.
        /// </summary>
        public List<IRangeSearchFilter> RangeFilters { get; } = new();

        protected void FlattenFilters(ICollection<ISearchFilter> filters)
        {
            foreach (var filter in filters)
            {
                if (filter is ICombinedSearchFilter combinedFilter)
                {
                    FlattenFilters(combinedFilter.Filters);
                }
                else
                {
                    Filters.Add(filter);

                    if (filter is IAttributeSearchFilter attrFilter)
                    {
                        if (attrFilter is IRangeSearchFilter rangeFilter)
                        {
                            RangeFilters.Add(rangeFilter);
                        }
                        else
                        {
                            AttributeFilters.Add(attrFilter);
                        }
                    }
                }
            }
        }
    }
}
