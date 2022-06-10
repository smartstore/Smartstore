namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Factory for registered scope managers.
    /// </summary>
    public interface IIndexScopeManager
    {
        /// <summary>
        /// Gets all index scope names.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> EnumerateScopes();

        /// <summary>
        /// Returns the instance of the first registered index scope provider (e.g. for "Catalog").
        /// </summary>
        /// <param name="scope">Index scope name to get provider for</param>
        /// <returns>The index scope provider implementation instance.</returns>
        IIndexScope GetIndexScope(string scope);
    }
}
