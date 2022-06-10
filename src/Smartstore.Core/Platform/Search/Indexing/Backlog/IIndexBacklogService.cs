namespace Smartstore.Core.Search.Indexing
{
    public interface IIndexBacklogService
    {
        DateTime ExpirationDate { get; }

        Task AddAsync(IndexBacklogItem[] items, CancellationToken cancelToken = default);
        Task<int> RemoveAsync(IndexBacklogItem[] items, CancellationToken cancelToken = default);
        Task<int> PurgeAsync(string scope, CancellationToken cancelToken = default);
        Task<int> GetBacklogCountAsync(string scope, DateTime? lastIndexedUtc);

        /// <summary>
        /// Removes all "Index" operation items followed by "Delete" for same entity.
        /// </summary>
        Task<IDictionary<int, IndexBacklogItem>> GetBacklogAsync(string scope, DateTime? lastIndexedUtc, CancellationToken cancelToken = default);
    }

    public static class IIndexBacklogServiceExtensions
    {
        public static Task<int> GetBacklogCountAsync(this IIndexBacklogService svc)
            => svc.GetBacklogCountAsync(null, null);

        public static Task<int> GetBacklogCountAsync(this IIndexBacklogService svc, string scope)
            => svc.GetBacklogCountAsync(scope, null);

        public static Task<int> GetBacklogCountAsync(this IIndexBacklogService svc, DateTime? lastIndexedUtc)
            => svc.GetBacklogCountAsync(null, lastIndexedUtc);

        public static Task<IDictionary<int, IndexBacklogItem>> GetBacklogAsync(this IIndexBacklogService svc)
            => svc.GetBacklogAsync(null, null);

        public static Task<IDictionary<int, IndexBacklogItem>> GetBacklogAsync(this IIndexBacklogService svc, string scope)
            => svc.GetBacklogAsync(scope, null);

        public static Task<IDictionary<int, IndexBacklogItem>> GetBacklogAsync(this IIndexBacklogService svc, DateTime? lastIndexedUtc)
            => svc.GetBacklogAsync(null, lastIndexedUtc);
    }
}
