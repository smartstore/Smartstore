namespace Smartstore.Core.Search.Indexing
{
    public interface IIndexDataSegmenter
    {
        /// <summary>
        /// Total number of documents.
        /// </summary>
        int TotalDocuments { get; }

        /// <summary>
        /// Number of documents per segment.
        /// </summary>
        int SegmentSize { get; }

        /// <summary>
        /// Gets current data segment.
        /// </summary>
        Task<IEnumerable<IIndexOperation>> GetCurrentSegmentAsync();

        /// <summary>
        /// Gets the page index of the current segment.
        /// </summary>
        int CurrentSegmentIndex { get; }

        /// <summary>
        /// Reads the next segment.
        /// </summary>
        /// <returns><c>true</c> if there are more segments, <c>false</c> if all segments have been processed.</returns>
        bool ReadNextSegment();
    }
}
