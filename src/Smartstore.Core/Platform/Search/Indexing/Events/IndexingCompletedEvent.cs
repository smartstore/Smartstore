namespace Smartstore.Core.Search.Indexing
{
    public class IndexingCompletedEvent
    {
        public IndexingCompletedEvent(IndexInfo indexInfo, bool success, bool wasRebuilt)
        {
            Guard.NotNull(indexInfo, nameof(indexInfo));

            IndexInfo = indexInfo;
            Success = success;
            WasRebuilt = wasRebuilt;
        }

        public IndexInfo IndexInfo { get; }

        public bool Success { get; }

        public bool WasRebuilt { get; }
    }
}