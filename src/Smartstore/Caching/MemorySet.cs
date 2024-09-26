#nullable enable

using System.Collections;
using System.Collections.Concurrent;

namespace Smartstore.Caching
{
    internal class OrderedMemorySet : MemorySet
    {
        /// <summary>
        /// Thread-safe queue for saving the insertion sequence
        /// </summary>
        private ConcurrentQueue<string> _orderTracker;

        /// <summary>
        /// Object for locking during deletion
        /// </summary>
        private readonly Lock _clearLock = new();

        public OrderedMemorySet(ICacheStore cache)
            : this(cache, null)
        {
        }

        public OrderedMemorySet(ICacheStore cache, IEnumerable<string>? values)
            : base(cache, values)
        {
            // Initialize the order tracker queue
            _orderTracker = values != null ? new ConcurrentQueue<string>(values) : new ConcurrentQueue<string>();
        }

        public override bool Add(string value)
        {
            if (base.Add(value))
            {
                // If the element is new, add it to the queue
                _orderTracker.Enqueue(value);
                return true;
            }

            return false;
        }

        public override void Clear()
        {
            lock (_clearLock)
            {
                base.Clear();
                // Reset the queue
                _orderTracker.Clear();
            }
        }

        protected override IEnumerator<string> GetEnumeratorImpl()
        {
            return _orderTracker.Where(Contains).GetEnumerator();
        }
    }

    internal class MemorySet : ISet
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

        public MemorySet(ICacheStore cache)
            : this(cache, null)
        {
        }

        public MemorySet(ICacheStore cache, IEnumerable<string>? values)
        {
            Guard.NotNull(cache);
            
            _cache = cache;
            _dictionary = values == null 
                ? new ConcurrentDictionary<string, bool>(DefaultConcurrencyLevel, DefaultCapacity)
                : new ConcurrentDictionary<string, bool>(DefaultConcurrencyLevel, values.Select(x => new KeyValuePair<string, bool>(x, false)), null);
        }

        public virtual bool Add(string value)
        {
            return _dictionary.TryAdd(value, false);
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

        public virtual void Clear()
        {
            _dictionary.Clear();
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

        protected virtual IEnumerator<string> GetEnumeratorImpl()
        {
            // PERF: Do not use dictionary.Keys here because that creates a snapshot
            // of the collection resulting in a List<T> allocation. Instead, use the
            // KeyValuePair enumerator and pick off the Key part.
            return _dictionary.Select(x => x.Key).GetEnumerator();
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