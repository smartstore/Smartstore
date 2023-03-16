namespace Smartstore.Core.Search.Indexing
{
    public sealed class NullIndexBacklogService : IIndexBacklogService
    {
        public static NullIndexBacklogService Instance { get; } = new();

        public DateTime ExpirationDate => DateTime.UtcNow.AddHours(-25);

        public Task AddAsync(IndexBacklogItem[] items, CancellationToken cancelToken = default)
            => Task.CompletedTask;

        public Task<IDictionary<int, IndexBacklogItem>> GetBacklogAsync(string scope, DateTime? lastIndexedUtc, CancellationToken cancelToken = default)
            => Task.FromResult<IDictionary<int, IndexBacklogItem>>(new Dictionary<int, IndexBacklogItem>());

        public Task<int> GetBacklogCountAsync(string scope, DateTime? lastIndexedUtc)
            => Task.FromResult(0);

        public Task<int> PurgeAsync(string scope, CancellationToken cancelToken = default)
            => Task.FromResult(0);

        public Task<int> RemoveAsync(IndexBacklogItem[] items, CancellationToken cancelToken = default)
            => Task.FromResult(0);
    }
}
