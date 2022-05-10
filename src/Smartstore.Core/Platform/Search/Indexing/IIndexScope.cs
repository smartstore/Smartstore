namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Provides all index scope related functions and services, e.g. "Catalog", "Forum" etc.
    /// </summary>
    public interface IIndexScope
    {
        /// <summary>
        /// The scope name that the provider implementation represents.
        /// </summary>
        string Scope { get; }
        
        /// <summary>
        /// TODO: (mg) (core) Describe
        /// </summary>
        IIndexCollector GetCollector();

        /// <summary>
        /// TODO: (mg) (core) Describe
        /// </summary>
        ISearchProvider GetSearchProvider();

        /// <summary>
        /// TODO: (mg) (core) Describe
        /// </summary>
        IIndexAnalyzer GetAnalyzer();
    }
}
