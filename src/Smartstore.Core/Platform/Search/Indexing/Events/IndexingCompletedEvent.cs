using Smartstore.Events;

namespace Smartstore.Core.Search.Indexing
{
    public class IndexingCompletedEvent : IEventMessage
    {
        public IndexingCompletedEvent(IndexInfo indexInfo, bool success, bool wasRebuilt)
        {
            Guard.NotNull(indexInfo);

            IndexInfo = indexInfo;
            Success = success;
            WasRebuilt = wasRebuilt;
        }

        public IndexInfo IndexInfo { get; }

        public bool Success { get; }

        public bool WasRebuilt { get; }
    }
}