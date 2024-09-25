#nullable enable

using System.Collections;
using System.Collections.Concurrent;

namespace Smartstore.Caching
{
    internal sealed class MemorySet : ISet
    {
        private readonly ICacheStore _cache;

        /// <summary>
        /// The default concurrency level is 2. That means the collection can cope with up to two
        /// threads making simultaneous modifications without blocking.
        /// Note ConcurrentDictionary's default concurrency level is dynamic, scaling according to
        /// the number of processors.
        /// </summary>
        private const int DefaultConcurrencyLevel = 2;

        /// <summary>
        /// Taken from ConcurrentDictionary.DEFAULT_CAPACITY
        /// </summary>
        private const int DefaultCapacity = 31;

        /// <summary>
        /// The backing dictionary. The values are never used; just the keys.
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> _dictionary;

        /// <summary>
        /// Whether the insertion sequence should be preserved while iterating.
        /// </summary>
        private readonly bool _preserveOrder;

        /// <summary>
        /// Thread-safe queue for saving the insertion sequence
        /// </summary>
        private ConcurrentQueue<string>? _orderTracker;

        /// <summary>
        /// Object for locking during deletion
        /// </summary>
        private readonly Lock _clearLock = new();

        public MemorySet(ICacheStore cache, bool preserveOrder = false)
            : this(cache, null, preserveOrder)
        {
        }

        public MemorySet(ICacheStore cache, IEnumerable<string>? values, bool preserveOrder = false)
        {
            _cache = cache;
            _preserveOrder = preserveOrder;

            if (values != null)
            {
                // Initialize the dictionary directly with the values
                _dictionary = new ConcurrentDictionary<string, bool>(DefaultConcurrencyLevel, values.Select(x => new KeyValuePair<string, bool>(x, false)), null);

                // Initialize the order tracker queue directly with the values
                if (_preserveOrder) _orderTracker = new ConcurrentQueue<string>(values);
            }
            else
            {
                _dictionary = new ConcurrentDictionary<string, bool>(DefaultConcurrencyLevel, DefaultCapacity);
                if (_preserveOrder) _orderTracker = new ConcurrentQueue<string>();
            }
        }

        public bool Add(string value)
        {
            Guard.NotEmpty(value);
            
            if (_dictionary.TryAdd(value, false)) 
            {
                // If the element is new, add it to the queue
                _orderTracker?.Enqueue(value);

                return true;
            }

            return false;
        }

        public void AddRange(IEnumerable<string> values)
        {
            if (values.IsNullOrEmpty())
            {
                return;
            }

            foreach (var v in values)
            {
                Add(v);
            }
        }

        public void Clear()
        {
            if (_orderTracker != null)
            {
                lock (_clearLock)
                {
                    _dictionary.Clear();

                    // Reset the queue
                    while (_orderTracker.TryDequeue(out _)) { }
                }
            }
            else
            {
                _dictionary.Clear();
            }
        }

        public bool Contains(string value)
        {
            return _dictionary.ContainsKey(value);
        }

        public bool Remove(string value)
        {
            return _dictionary.TryRemove(value, out _);
        }

        public bool Move(string destinationKey, string value)
        {
            var target = _cache?.GetHashSet(destinationKey);
            if (target != null)
            {
                if (_dictionary.TryRemove(value, out _))
                {
                    return target.Add(value);
                } 
            }

            return false;
        }

        public int Count 
        { 
            get => _dictionary.Count;
        }

        public long UnionWith(params string[] keys)
        {
            return Combine(this.Union, keys);
        }

        public long IntersectWith(params string[] keys)
        {
            return Combine(this.Intersect, keys);
        }

        public long ExceptWith(params string[] keys)
        {
            return Combine(this.Except, keys);
        }

        private long Combine(Func<IEnumerable<string>, IEnumerable<string>> func, params string[] keys)
        {
            if (keys.Length == 0)
            {
                return 0;
            }  

            var other = keys.SelectMany(x => _cache?.GetHashSet(x) ?? Enumerable.Empty<string>()).Distinct();
            var result = func(other);

            Clear();
            AddRange(result);

            return _dictionary.Count;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        private IEnumerator<string> GetEnumeratorImpl()
        {
            // PERF: Do not use dictionary.Keys here because that creates a snapshot
            // of the collection resulting in a List<T> allocation. Instead, use the
            // KeyValuePair enumerator and pick off the Key part.

            var items = _orderTracker != null
                ? _orderTracker.Where(_dictionary.ContainsKey)
                : _dictionary.Select(x => x.Key);

            return items.GetEnumerator();
        }

        #region Async

        public Task<bool> AddAsync(string value)
        {
            return Task.FromResult(Add(value));
        }

        public Task ClearAsync()
        {
            Clear();
            return Task.CompletedTask;
        }

        public Task<bool> ContainsAsync(string value)
        {
            return Task.FromResult(Contains(value));
        }

        public Task<bool> RemoveAsync(string value)
        {
            return Task.FromResult(Remove(value));
        }

        public Task AddRangeAsync(IEnumerable<string> values)
        {
            AddRange(values);
            return Task.CompletedTask;
        }

        public Task<bool> MoveAsync(string destinationKey, string value)
        {
            return Task.FromResult(Move(destinationKey, value));
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