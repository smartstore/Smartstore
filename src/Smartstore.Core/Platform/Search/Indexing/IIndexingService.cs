using Smartstore.Scheduling;

namespace Smartstore.Core.Search.Indexing
{
    public interface IIndexingService
    {
        Task BuildIndexAsync(string scope, TaskExecutionContext context, string command, CancellationToken cancelToken = default);
        Task DeleteIndexAsync(string scope, CancellationToken cancelToken = default);

        Task<IndexInfo> GetIndexInfoAsync(string scope, bool force = false);
        Task SaveIndexInfoAsync(IndexInfo info);
    }
}
