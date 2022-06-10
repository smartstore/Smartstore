namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Factory for registered search index providers
    /// </summary>
    public interface IIndexManager
    {
        /// <summary>
        /// Gets a value indicating whether at least one provider is available that implements <see cref="IIndexProvider"/>.
        /// </summary>
        /// <param name="scope">Index scope name</param>
        /// <param name="activeOnly">A value indicating whether only active providers should be queried for.</param>
        /// <returns><c>true</c> if at least one provider is registered, <c>false</c> ortherwise.</returns>
        /// <remarks>Primarily used to skip indexing processes.</remarks>
        bool HasAnyProvider(string scope, bool activeOnly = true);

        /// <summary>
        /// Returns the instance of the first registered index provider (e.g. a Lucene provider).
        /// </summary>
        /// <param name="scope">Index scope name</param>
        /// <param name="activeOnly">A value indicating whether only active providers should be queried for.</param>
        /// <returns>The index provider implementation instance.</returns>
        IIndexProvider GetIndexProvider(string scope, bool activeOnly = true);
    }
}
