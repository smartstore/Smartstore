namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Represents a provider of a search index.
    /// </summary>
    public interface IIndexProvider
    {
        /// <summary>
        /// Gets a value indicating whether the search index given by scope is active
        /// </summary>
        bool IsActive(string scope);

        /// <summary>
        /// Enumerates the names of all EXISTING indexes. 
        /// A name is required for the <see cref="GetIndexStore(string)"/> method.
        /// </summary>
        Task<IEnumerable<string>> EnumerateIndexesAsync();

        /// <summary>
        /// Returns a provider specific implementation of the <see cref="IIndexStore"/> interface
        /// which allows interaction with the underlying index store for managing the index and containing documents.
        /// </summary>
        /// <param name="scope">The index name</param>
        /// <returns>The index store</returns>
        /// <remarks>
        /// This methods always returns an object instance, even if the index does not exist yet.
        /// </remarks>
        IIndexStore GetIndexStore(string scope);

        /// <summary>
        /// Returns a provider specific implementation of the <see cref="ISearchEngine"/> interface
        /// which allows executing queries against an index store.
        /// </summary>
        /// <param name="store">The index store</param>
        /// <param name="query">The query to execute against the store</param>
        /// <returns>The search engine instance</returns>
        ISearchEngine GetSearchEngine(IIndexStore store, ISearchQuery query);
    }
}
