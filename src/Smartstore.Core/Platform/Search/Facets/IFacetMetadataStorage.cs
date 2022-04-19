namespace Smartstore.Core.Search.Facets
{
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
    }
}
