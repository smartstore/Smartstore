using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Search
{
    public abstract partial class SearchProviderBase : ISearchProvider
    {
        /// <inheritdoc/>
        public virtual SearchSort GetSorting(SearchSort sort, ISearchQuery query)
            => null;

        /// <inheritdoc/>
        public virtual float GetBoost(ISearchFilter filter)
            => filter.Boost;

        /// <inheritdoc/>
        public virtual string GetLocalizedFieldName(ISearchFilter filter, string languageCulture)
            => null;

        /// <inheritdoc/>
        public virtual bool IsFacetField(string fieldName, ISearchQuery query)
            => false;

        /// <inheritdoc/>
        public abstract IList<SearchField> GetFields(ISearchQuery query, string languageCulture);

        /// <inheritdoc/>
        public abstract IDictionary<string, FacetGroup> GetFacetMap(ISearchEngine searchEngine, IFacetMetadataStorage storage);

        /// <inheritdoc/>
        public abstract Task<IDictionary<string, FacetGroup>> GetFacetMapAsync(ISearchEngine searchEngine, IFacetMetadataStorage storage);
    }
}
