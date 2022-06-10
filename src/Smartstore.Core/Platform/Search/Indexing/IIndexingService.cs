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

    public class NullIndexingService : IIndexingService
    {
        public static IIndexingService Instance => new NullIndexingService();

        public Task BuildIndexAsync(string scope, TaskExecutionContext context, string command, CancellationToken cancelToken = default)
            => Task.CompletedTask;

        public Task DeleteIndexAsync(string scope, CancellationToken cancelToken = default)
            => Task.CompletedTask;

        public Task<IndexInfo> GetIndexInfoAsync(string scope, bool force = false)
            => Task.FromResult(new IndexInfo(scope));

        public Task SaveIndexInfoAsync(IndexInfo info)
            => Task.CompletedTask;
    }
}
