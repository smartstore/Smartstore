using System.Collections;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;
using Smartstore.Data;
using Smartstore.Domain;
using Smartstore.Events;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Caching
{
    public class MemoryCacheStore : Disposable, IMemoryCacheStore
    {
        private readonly IOptions<MemoryCacheOptions> _optionsAccessor;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessageBus _bus;
        private readonly ICollection<string> _keys = new SyncedCollection<string>(new HashSet<string>()) { ReadLockFree = true };

        private MemoryCache _cache;

        public MemoryCacheStore(IOptions<MemoryCacheOptions> optionsAccessor, IMessageBus bus)
            : this(optionsAccessor, bus, NullLoggerFactory.Instance)
        {
        }

        public MemoryCacheStore(IOptions<MemoryCacheOptions> optionsAccessor, IMessageBus bus, ILoggerFactory loggerFactory)
        {
            Guard.NotNull(optionsAccessor, nameof(optionsAccessor));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            _optionsAccessor = optionsAccessor;
            _bus = bus;
            _loggerFactory = loggerFactory;

            _cache = CreateCache();

            // Subscribe to cache events sent by other nodes in a web farm
            _bus.Subscribe("cache", OnCacheEvent);
        }

        public event EventHandler<CacheEntryRemovedEventArgs> Removed;

        private void OnCacheEvent(string channel, string message)
        {
            var parameter = string.Empty;
            string action;

            var index = message.IndexOf('^');
            if (index >= 0 && index < message.Length - 1)
            {
                action = message.Substring(0, index);
                parameter = message[(index + 1)..];
            }
            else
            {
                action = message;
            }

            switch (action)
            {
                case "clear":
                    Clear();
                    break;
                case "remove":
                    Remove(parameter);
                    break;
                case "removebypattern":
                    RemoveByPattern(parameter);
                    break;
            }
        }

        private MemoryCache CreateCache()
        {
            return new MemoryCache(_optionsAccessor.Value, _loggerFactory);
        }

        public bool IsDistributed { get; }

        public virtual bool Contains(string key)
            => _keys.Contains(key);

        public virtual Task<bool> ContainsAsync(string key)
            => Task.FromResult(Contains(key));

        public virtual CacheEntry Get(string key)
        {
            var entry = _cache.Get<CacheEntry>(key);
            if (entry != null)
            {
                entry.LastAccessedOn = DateTimeOffset.UtcNow;
            }

            return entry;
        }

        public virtual Task<CacheEntry> GetAsync(string key)
            => Task.FromResult(Get(key));

        public virtual ISet GetHashSet(string key, Func<IEnumerable<string>> acquirer = null)
        {
            var result = _cache.GetOrCreate(key, x =>
            {
                _keys.Add(key);

                var memSet = new MemorySet(this);
                var items = acquirer?.Invoke();
                if (items != null)
                {
                    memSet.AddRange(items);
                }

                return new CacheEntry { Key = key, Value = memSet, ValueType = typeof(MemorySet) };
            });

            return result.Value as ISet;
        }

        public virtual async Task<ISet> GetHashSetAsync(string key, Func<Task<IEnumerable<string>>> acquirer = null)
        {
            var result = await _cache.GetOrCreateAsync(key, async (x) =>
            {
                _keys.Add(key);

                var memSet = new MemorySet(this);
                if (acquirer != null)
                {
                    var items = await acquirer.Invoke();
                    if (items != null)
                    {
                        memSet.AddRange(items);
                    }
                }

                return new CacheEntry { Key = key, Value = memSet, ValueType = typeof(MemorySet) };
            });

            return result.Value as ISet;
        }

        public void Put(string key, CacheEntry entry)
        {
            entry.Key = key;
            PopulateCacheEntry(entry, _cache.CreateEntry(key));
        }

        public Task PutAsync(string key, CacheEntry entry)
        {
            Put(key, entry);
            return Task.CompletedTask;
        }

        protected virtual void PopulateCacheEntry(CacheEntry item, ICacheEntry entry)
        {
            _keys.Add((string)entry.Key);

            TryDropLazyLoader(item.Value);

            entry.SetValue(item);
            entry.SetPriority((CacheItemPriority)item.Priority);

            if (item.ApplyTimeExpirationPolicy)
            {
                if (item.AbsoluteExpiration != null)
                {
                    entry.SetAbsoluteExpiration(item.AbsoluteExpiration.Value);
                }

                if (item.SlidingExpiration != null)
                {
                    entry.SetSlidingExpiration(item.SlidingExpiration.Value);
                }
            }

            // Ensure that cancelling the token removes this item from the cache
            entry.AddExpirationToken(new CancellationChangeToken(item.CancellationTokenSource.Token));

            if (item.Dependencies != null && item.Dependencies.Length > 0)
            {
                // INFO: we can only depend on existing items, otherwise this entry will be removed immediately.
                var dependantEntries = item.Dependencies
                    .Select(x => _cache.Get<CacheEntry>(x))
                    .Where(x => x != null)
                    .ToArray();

                foreach (var dep in dependantEntries)
                {
                    entry.AddExpirationToken(new CancellationChangeToken(dep.CancellationTokenSource.Token));
                }
            }

            // Ensure that when this item is expired, any objects depending on the token are also expired
            entry.RegisterPostEvictionCallback((object key, object value, EvictionReason reason, object state) =>
            {
                var entry = (value as CacheEntry);

                if (reason != EvictionReason.Replaced)
                {
                    var source = entry.CancellationTokenSource;

                    _keys.Remove((string)key);

                    if (entry.CancelTokenSourceOnRemove && !source.IsCancellationRequested)
                    {
                        source.Cancel();
                    }
                    else
                    {
                        source.Dispose();
                    }
                }

                Removed?.Invoke(this, new CacheEntryRemovedEventArgs
                {
                    Key = (string)key,
                    Reason = (CacheEntryRemovedReason)reason,
                    Entry = entry
                });
            });

            entry.Dispose();
        }

        internal static void TryDropLazyLoader(object value)
        {
            if (value is BaseEntity entity)
            {
                entity.LazyLoader = NullLazyLoader.Instance;
            }
            else if (value is IEnumerable e)
            {
                foreach (var item in e.OfType<BaseEntity>())
                {
                    item.LazyLoader = NullLazyLoader.Instance;
                }
            }
        }

        public virtual void Remove(string key)
        {
            _cache.Remove(key);
        }

        public virtual Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public virtual long RemoveByPattern(string pattern)
        {
            lock (_cache)
            {
                // Lock atomic operation
                var keysToRemove = Keys(pattern);
                int numRemoved = 0;

                foreach (string key in keysToRemove)
                {
                    _cache.Remove(key);
                    numRemoved++;
                }

                return numRemoved;
            }
        }

        public virtual Task<long> RemoveByPatternAsync(string pattern)
            => Task.FromResult(RemoveByPattern(pattern));

        public virtual IEnumerable<string> Keys(string pattern = "*")
        {
            if (pattern.IsEmpty() || pattern == "*")
            {
                return _keys.ToArray();
            }

            var wildcard = new Wildcard(pattern, RegexOptions.IgnoreCase);
            return _keys
                .Where(x => wildcard.IsMatch(x))
                .ToArray();
        }

        public virtual IAsyncEnumerable<string> KeysAsync(string pattern = "*")
            => Keys(pattern).ToAsyncEnumerable();

        public IDistributedLock GetLock(string key)
            => new DistributedSemaphoreLock("memcache:" + key);

        public virtual ILockHandle AcquireKeyLock(string key, CancellationToken cancelToken = default)
            => AsyncLock.Keyed("memcache:" + key, TimeSpan.FromSeconds(5), cancelToken);

        public virtual Task<ILockHandle> AcquireAsyncKeyLock(string key, CancellationToken cancelToken = default)
            => AsyncLock.KeyedAsync("memcache:" + key, TimeSpan.FromSeconds(5), cancelToken);

        public virtual void Clear()
        {
            _keys.Clear();

            // Faster way of clearing cache: https://stackoverflow.com/questions/8043381/how-do-i-clear-a-system-runtime-caching-memorycache
            var oldCache = Interlocked.Exchange(ref _cache, CreateCache());
            oldCache.Dispose();
            GC.Collect();
        }

        public virtual Task ClearAsync()
        {
            Clear();
            return Task.CompletedTask;
        }

        public virtual TimeSpan? GetTimeToLive(string key)
            => Get(key)?.GetTimeToLive();

        public virtual Task<TimeSpan?> GetTimeToLiveAsync(string key)
            => Task.FromResult(GetTimeToLive(key));

        public virtual bool SetTimeToLive(string key, TimeSpan? duration)
        {
            var entry = Get(key);
            if (entry != null && entry.GetTimeToLive() != duration)
            {
                var clone = entry.Clone();
                clone.AbsoluteExpiration = duration;
                clone.CancellationTokenSource = entry.CancellationTokenSource;

                Put(key, clone);
            }

            return false;
        }

        public virtual Task<bool> SetTimeToLiveAsync(string key, TimeSpan? duration)
            => Task.FromResult(SetTimeToLive(key, duration));

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _keys.Clear();
                _cache.Dispose();
            }
        }
    }
}
