using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Search
{
    public abstract partial class SearchProviderBase : ISearchProvider
    {
        public virtual IList<SearchSort> GetSorting(SearchSort sort, ISearchQuery query)
            => null;

        public virtual float? GetBoost(ISearchFilter filter)
            => filter.Boost;

        public virtual string GetLocalizedFieldName(ISearchFilter filter, string languageCulture)
            => null;

        public virtual bool IsFacetField(string fieldName, ISearchQuery query)
            => false;

        public abstract IDictionary<string, FacetGroup> GetFacetMap(ISearchEngine searchEngine, IFacetMetadataStorage storage);

        public abstract Task<IDictionary<string, FacetGroup>> GetFacetMapAsync(ISearchEngine searchEngine, IFacetMetadataStorage storage);
    }
}
