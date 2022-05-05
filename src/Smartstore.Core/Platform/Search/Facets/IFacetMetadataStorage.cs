namespace Smartstore.Core.Search.Facets
{
    public class LoadFacetMetadataContext
    {
        public bool Cache { get; init; }
        public string FieldName { get; init; }
        public string DocumentType { get; init; }
        
        public ISearchQuery Query { get; init; } = new SearchQuery();
        public string[] ApplyOriginalFilters { get; init; } = Array.Empty<string>();
        
        public LoadFacetMetadataContext ParentContext { get; init; }
        public Func<ISearchHit, MetadataCreatorContext, FacetMetadata> MetadataCreator { get; init; }
    }

    public class MetadataCreatorContext
    {
        public ISearchEngine SearchEngine { get; init; }
        public LoadFacetMetadataContext Context { get; init; }
        public IDictionary<object, FacetMetadata> ParentData { get; init; }
    }

    // TODO: (mg) (core) add storing of facet metadata to IMetadataStorage.
    /// <summary>
    /// Loading of facet metadata. Metadata is stored during indexing and loaded when searching with facets.
    /// </summary>
    public interface IFacetMetadataStorage
    {
        /// <summary>
        /// Loads facet metadata.
        /// </summary>
        /// <param name="searchEngine">Search engine instance.</param>
        /// <param name="descriptor">Facet descriptor.</param>
        /// <returns>Facet metadata for a facet descriptor.</returns>
        Task<IDictionary<object, FacetMetadata>> LoadAsync(ISearchEngine searchEngine, FacetDescriptor descriptor);

        Task<IDictionary<object, FacetMetadata>> LoadAsync(ISearchEngine searchEngine, LoadFacetMetadataContext context);
    }
}
