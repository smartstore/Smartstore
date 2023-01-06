using System.Collections;
using Smartstore.Collections;
using Smartstore.Threading;

namespace Smartstore.Caching
{
    [Serializable]
    internal class MemorySet : ISet
    {
        private readonly ICacheStore _cache;
        private readonly HashSet<string> _set = new(StringComparer.OrdinalIgnoreCase);
        private readonly SyncedCollection<string> _safeSet;

        public MemorySet(ICacheStore cache)
        {
            _cache = cache;
            _safeSet = _set.AsSynchronized();
        }

        public bool Add(string item)
        {
            using (_safeSet.Lock.GetWriteLock())
            {
                return _set.Add(item);
            }
        }

        public void AddRange(IEnumerable<string> items)
        {
            using (_safeSet.Lock.GetWriteLock())
            {
                _set.UnionWith(items);
            } 
        }

        public void Clear()
        {
            _safeSet.Clear();
        }

        public bool Contains(string item)
        {
            return _safeSet.Contains(item);
        }

        public bool Remove(string item)
        {
            return _safeSet.Remove(item);
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

        public int Count 
        { 
            get => _safeSet.Count;
        }

        public long UnionWith(params string[] keys)
        {
            return Combine(_set.UnionWith, keys);
        }

        public long IntersectWith(params string[] keys)
        {
            return Combine(_set.IntersectWith, keys);
        }

        public long ExceptWith(params string[] keys)
        {
            return Combine(_set.ExceptWith, keys);
        }

        private long Combine(Action<IEnumerable<string>> action, params string[] keys)
        {
            if (keys.Length == 0)
            {
                return 0;
            }  

            var other = keys.SelectMany(x => _cache?.GetHashSet(x) ?? Enumerable.Empty<string>()).Distinct();

            using (_safeSet.Lock.GetWriteLock())
            {
                action(other);
            }

            return Count;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _safeSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _safeSet.GetEnumerator();
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