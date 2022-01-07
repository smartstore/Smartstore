namespace Smartstore.Caching
{
    public interface ISet : IEnumerable<string>
    {
        bool Add(string item);
        void AddRange(IEnumerable<string> items);
        void Clear();
        bool Contains(string item);
        bool Remove(string item);
        bool Move(string destinationKey, string item);

        long UnionWith(params string[] keys);
        long IntersectWith(params string[] keys);
        long ExceptWith(params string[] keys);

        int Count { get; }

        Task<bool> AddAsync(string item);
        Task AddRangeAsync(IEnumerable<string> items);
        Task ClearAsync();
        Task<bool> ContainsAsync(string item);
        Task<bool> RemoveAsync(string item);
        Task<bool> MoveAsync(string destinationKey, string item);
        Task<long> UnionWithAsync(params string[] keys);
        Task<long> IntersectWithAsync(params string[] keys);
        Task<long> ExceptWithAsync(params string[] keys);
    }
}
