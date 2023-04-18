#nullable enable

namespace Smartstore.Core.Search
{
    public class CombinedSearchFilter : SearchFilterBase, ICombinedSearchFilter
    {
        public CombinedSearchFilter()
            : this((string?)null)
        {
        }

        public CombinedSearchFilter(string? name)
        {
            FieldName = name;
            Filters = new List<ISearchFilter>();
        }

        public CombinedSearchFilter(IEnumerable<ISearchFilter> filters)
            : this(null, filters)
        {
        }

        public CombinedSearchFilter(string? name, IEnumerable<ISearchFilter> filters)
        {
            Guard.NotNull(filters);

            FieldName = name;
            Filters = new List<ISearchFilter>(filters);
        }

        public ICollection<ISearchFilter> Filters { get; }

        public CombinedSearchFilter Add(ISearchFilter filter)
        {
            Guard.NotNull(filter);

            Filters.Add(filter);

            return this;
        }

        public override string ToString()
        {
            if (Filters.Count > 0)
            {
                return string.Concat(
                    FieldName.RightPad(),
                    "(",
                    string.Join(", ", Filters.Select(x => x.ToString())), 
                    ")");
            }

            return string.Empty;
        }
    }
}
