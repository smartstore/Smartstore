namespace Smartstore.Core.Search.Indexing
{
    public class NullIndexBacklogService : IIndexBacklogService
    {
        public static IIndexBacklogService Instance => new NullIndexBacklogService();

        public DateTime ExpirationDate => DateTime.UtcNow.AddHours(-25);

        public Task AddAsync(params IndexBacklogItem[] items)
            => Task.CompletedTask;

        public Task<IDictionary<int, IndexBacklogItem>> GetBacklogAsync(string scope, DateTime? lastIndexedUtc)
            => Task.FromResult<IDictionary<int, IndexBacklogItem>>(new Dictionary<int, IndexBacklogItem>());

        public Task<int> GetBacklogCountAsync(string scope, DateTime? lastIndexedUtc)
            => Task.FromResult(0);

        public Task<int> PurgeAsync(string scope)
            => Task.FromResult(0);

        public Task<int> RemoveAsync(params IndexBacklogItem[] items)
            => Task.FromResult(0);
    }
}
