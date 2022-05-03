using Smartstore.Scheduling;

namespace Smartstore.Core.Search.Indexing
{
    /// <summary>
    /// Represents a data collector of a search index.
    /// </summary>
    public interface IIndexCollector
    {
        /// <summary>
        /// Name of the search index, e.g. "Catalog".
        /// </summary>
        string Scope { get; }

        /// <summary>
        /// Creates an <see cref="IndexInfo"/> instance.
        /// </summary>
        /// <returns><see cref="IndexInfo"/> instance.</returns>
        IndexInfo CreateIndexInfo();

        /// <summary>
        /// Create a context for writing to a search index.
        /// </summary>
        /// <param name="reason">Reason for acquirement.</param>
        /// <param name="taskContext">Indexing task context.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        /// <returns>Context for writing to a search index.</returns>
        Task<AcquireWriterContext> CreateWriterContextAsync(AcquirementReason reason, TaskExecutionContext taskContext, CancellationToken cancelToken = default);

        /// <summary>
        /// Collects data to include in the search index.
        /// </summary>
        /// <param name="context">Context for writing to a search index.</param>
        /// <param name="lastIndexedUtc">Date of last indexing (in UTC).</param>
        /// <param name="continueDocumentId">
        /// ID of the last processed index document. Typically <see cref="BaseEntity.Id"/>.
        /// Used to continue indexing at the point where it was aborted.
        /// </param>
        /// <param name="newDocument">Function to create an <see cref="IIndexDocument"/> instance.</param>
        Task<IndexCollectorResult> CollectAsync(
            AcquireWriterContext context,
            DateTime? lastIndexedUtc,
            int continueDocumentId,
            // TODO: (mg) (core) Replace obsolete SearchDocumentType enum with "SearchDocumentTypes" static class
            // that provides public consts for KNOWN types. Working with strings is awkward.
            Func<int, string, IIndexDocument> newDocument);
    }
}
