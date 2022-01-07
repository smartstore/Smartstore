namespace Smartstore.Caching
{
    [Serializable]
    internal class MemorySet : HashSet<string>, ISet
    {
        private readonly ICacheStore _cache;

        public MemorySet(ICacheStore cache)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            _cache = cache;
        }

        public void AddRange(IEnumerable<string> items)
        {
            base.UnionWith(items);
        }

        public bool Move(string destinationKey, string item)
        {
            if (Contains(item))
            {
                var target = _cache?.GetHashSet(destinationKey);
                if (target != null)
                {
                    return target.Add(item);
                }
            }

            return false;
        }

        public long ExceptWith(params string[] keys)
        {
            return Combine(x => this.Except(x), keys);
        }

        public long IntersectWith(params string[] keys)
        {
            return Combine(x => this.Intersect(x), keys);
        }

        public long UnionWith(params string[] keys)
        {
            return Combine(x => this.Union(x), keys);
        }

        private long Combine(Func<IEnumerable<string>, IEnumerable<string>> func, params string[] keys)
        {
            if (keys.Length == 0)
                return 0;

            var other = keys.SelectMany(x => _cache?.GetHashSet(x) ?? Enumerable.Empty<string>()).Distinct();
            var result = func(other);

            Clear();
            AddRange(other);

            return Count;
        }

        #region Async

        public Task<bool> AddAsync(string item)
        {
            return Task.FromResult(Add(item));
        }

        public Task ClearAsync()
        {
            Clear();
            return Task.CompletedTask;
        }

        public Task<bool> ContainsAsync(string item)
        {
            return Task.FromResult(Contains(item));
        }

        public Task<bool> RemoveAsync(string item)
        {
            return Task.FromResult(Remove(item));
        }

        public Task AddRangeAsync(IEnumerable<string> items)
        {
            AddRange(items);
            return Task.CompletedTask;
        }

        public Task<bool> MoveAsync(string destinationKey, string item)
        {
            return Task.FromResult(Move(destinationKey, item));
        }

        public Task<long> ExceptWithAsync(params string[] keys)
        {
            return Task.FromResult(ExceptWith(keys));
        }

        public Task<long> IntersectWithAsync(params string[] keys)
        {
            return Task.FromResult(IntersectWith(keys));
        }

        public Task<long> UnionWithAsync(params string[] keys)
        {
            return Task.FromResult(UnionWith(keys));
        }

        #endregion
    }
}