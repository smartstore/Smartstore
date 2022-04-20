using Smartstore.Scheduling;

namespace Smartstore.Core.Search.Indexing
{
    public class NullIndexingService : IIndexingService
    {
        public static IIndexingService Instance => new NullIndexingService();

        public IEnumerable<string> EnumerateScopes()
            => Enumerable.Empty<string>();

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
