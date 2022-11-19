using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Represents metadata for registration of <see cref="IIndexScope"/> implementations.
    /// </summary>
    public class IndexScopeMetadata
    {
        /// <summary>
        /// The name of the index scope, e.g. "Catalog", "Forum" etc.
        /// </summary>
        public string Name { get; set; }
    }

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
        /// Gets the widget invoker for optional configuration. Return <c>null</c> when there is nothing to render.
        /// </summary>
        Widget GetConfigurationWidget();

        /// <summary>
        /// Gets an <see cref="IndexInfo"/> instance which provides base information about the index.
        /// </summary>
        /// <returns><see cref="IndexInfo"/> instance.</returns>
        IndexInfo GetIndexInfo();

        /// <summary>
        /// Gets the data collector.
        /// It provides all data to be added to the search index.
        /// </summary>
        IIndexCollector GetCollector();

        /// <summary>
        /// Gets the search provider.
        /// It provides search index specific information such as fields to search, sorting and faceting.
        /// For example, <see cref="ISearchProvider"/> is used to specify the boosting of certain search fields.
        /// </summary>
        ISearchProvider GetSearchProvider();

        /// <summary>
        /// Gets the index analyzer. It provides information for text analysis of certain search fields.
        /// For example, <see cref="IIndexAnalyzer"/> is used to specify that product SKUs are to be analyzed as keywords.
        /// </summary>
        IIndexAnalyzer GetAnalyzer();
    }
}
