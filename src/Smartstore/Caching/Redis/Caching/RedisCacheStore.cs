using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Smartstore.Caching;
using StackExchange.Redis;

namespace Smartstore.Redis.Caching
{
    public class RedisCacheStore : Disposable, IDistributedCacheStore
    {
        private readonly IRedisConnectionFactory _connectionFactory;
        private readonly ConnectionMultiplexer _multiplexer;
        private readonly RedisMessageBus _messageBus;
        private readonly IRedisSerializer _serializer;

        private readonly string _cachePrefix;
        private readonly string _keyPrefix;

        public RedisCacheStore(IRedisConnectionFactory connectionFactory, IRedisSerializer serializer)
        {
            // TODO: (core) Build versionStr from SmartstoreVersion class
            var versionStr = "5.0.0";

            // Don't try to deserialize values created with older app versions (could be incompatible)
            _cachePrefix = "cache." + versionStr + ":";
            _keyPrefix = BuildCacheKey("");

            _connectionFactory = connectionFactory;
            _multiplexer = _connectionFactory.GetConnection(_connectionFactory.GetConnectionString("Smartstore.Redis.Cache"));
            _messageBus = _connectionFactory.GetMessageBus(_connectionFactory.GetConnectionString("Smartstore.Redis.MessageBus"));
            _serializer = serializer;

            // Subscribe to key events triggered by Redis on item expiration
            _messageBus.SubscribeToKeyEvent("expired", OnRedisKeyEvent);
            _messageBus.SubscribeToKeyEvent("evicted", OnRedisKeyEvent);
        }

        #region Pub/Sub

        public event EventHandler<CacheEntryExpiredEventArgs> Expired;

        private void OnRedisKeyEvent(string channel, string message)
        {
            // INFO: "message" is the cache key

            var key = RedisUtility.GetEventFromChannel(channel);
            switch (key)
            {
                //case "expire":
                case "expired":
                case "evicted":
                    Expired?.Invoke(this, new CacheEntryExpiredEventArgs { Key = message });
                    RemoveDependingEntries(new string[] { message });
                    //Debug.WriteLine("Expiration occurred for {0}".FormatInvariant(message));
                    break;
            }
        }

        #endregion

        public bool IsDistributed
            => true;

        public IDatabase Database 
            => _multiplexer.GetDatabase();

        public IRedisSerializer Serializer
            => _serializer;

        public Task<IDisposable> AcquireAsyncKeyLock(string key, CancellationToken cancelToken = default)
        {
            return new RedisLock(Database, BuildCacheKey(key)).LockAsync(cancelToken: cancelToken);
        }

        public IDisposable AcquireKeyLock(string key)
        {
            return new RedisLock(Database, BuildCacheKey(key)).Lock();
        }

        public void Clear()
        {
            RedisAction(true, () =>
            {
                _multiplexer.KeyDeleteWithPattern(BuildCacheKey("*"));
                PostMessage("clear");
            });
        }

        public async Task ClearAsync()
        {
            await RedisActionAsync(true, async () =>
            {
                await _multiplexer.KeyDeleteWithPatternAsync(BuildCacheKey("*"));
                await PostMessageAsync("clear");
            });
        }

        public bool Contains(string key)
        {
            // INFO: No RedisAction for perf reasons.
            return Database.KeyExists(BuildCacheKey(key));
        }

        public Task<bool> ContainsAsync(string key)
        {
            // INFO: No RedisAction for perf reasons.
            return Database.KeyExistsAsync(BuildCacheKey(key));
        }

        public CacheEntry Get(string key)
        {
            // INFO: No RedisAction for perf reasons.
            return Database.ObjectGet<CacheEntry>(_serializer, BuildCacheKey(key));
        }

        public Task<CacheEntry> GetAsync(string key)
        {
            // INFO: No RedisAction for perf reasons.
            return Database.ObjectGetAsync<CacheEntry>(_serializer, BuildCacheKey(key));
        }

        public ISet GetHashSet(string key, Func<IEnumerable<string>> acquirer = null)
        {
            var redisKey = BuildCacheKey(key);
            var set = new RedisHashSet(redisKey, this);

            if (!Database.KeyExists(redisKey))
            {
                var items = acquirer?.Invoke();
                if (items != null)
                {
                    set.AddRange(items);
                }
            }

            return set;
        }

        public async Task<ISet> GetHashSetAsync(string key, Func<Task<IEnumerable<string>>> acquirer = null)
        {
            var redisKey = BuildCacheKey(key);
            var set = new RedisHashSet(redisKey, this);

            if (acquirer != null && !(await Database.KeyExistsAsync(redisKey)))
            {
                var items = await acquirer.Invoke().ConfigureAwait(false);
                if (items != null)
                {
                    await set.AddRangeAsync(items).ConfigureAwait(false);
                }
            }

            return set;
        }

        public IEnumerable<string> Keys(string pattern = "*")
        {
            IEnumerable<string> keys = null;

            RedisAction(true, () =>
            {
                keys = _multiplexer.GetKeys(BuildCacheKey(pattern)).Select(x => NormalizeKey(x));
            });

            return keys;
        }

        public IAsyncEnumerable<string> KeysAsync(string pattern = "*")
        {
            IAsyncEnumerable<string> keys = null;

            RedisAction(true, () =>
            {
                keys = _multiplexer.GetKeysAsync(BuildCacheKey(pattern)).Select(x => NormalizeKey(x));
            });

            return keys;
        }

        public void Put(string key, CacheEntry entry)
        {
            var condition = _serializer.CanSerialize(entry) && (_serializer.CanDeserialize(entry.Value?.GetType()));
            RedisAction(condition, () =>
            {
                Database.ObjectSet(_serializer, BuildCacheKey(key), entry, entry.Duration);
                if (entry.Dependencies != null && entry.Dependencies.Any())
                {
                    EnlistDependencyKeys(key, entry.Dependencies);
                }

                // Other nodes must remove this entry from their local cache
                PostMessage("remove^" + key);
            });
        }

        public async Task PutAsync(string key, CacheEntry entry)
        {
            var condition = _serializer.CanSerialize(entry) && (_serializer.CanDeserialize(entry.Value?.GetType()));
            await RedisActionAsync(condition, async () =>
            {
                await Database.ObjectSetAsync(_serializer, BuildCacheKey(key), entry, entry.Duration).ConfigureAwait(false);
                if (entry.Dependencies != null && entry.Dependencies.Any())
                {
                    await EnlistDependencyKeysAsync(key, entry.Dependencies).ConfigureAwait(false);
                }

                // Other nodes must remove this entry from their local cache
                await PostMessageAsync("remove^" + key);
            });
        }

        public void Remove(string key)
        {
            RedisAction(true, () =>
            {
                Database.KeyDelete(BuildCacheKey(key));
                RemoveDependingEntries(new string[] { key });
                PostMessage("remove^" + key);
            });
        }

        public async Task RemoveAsync(string key)
        {
            await RedisActionAsync(true, async () =>
            {
                await Database.KeyDeleteAsync(BuildCacheKey(key));
                await RemoveDependingEntriesAsync(new string[] { key });
                await PostMessageAsync("remove^" + key);
            });
        }

        public long RemoveByPattern(string pattern)
        {
            int numRemoved = 0;

            RedisAction(true, () =>
            {
                var keys = _multiplexer.GetKeys(BuildCacheKey(pattern)).Select(x => (RedisKey)x).ToArray();
                numRemoved = keys.Length;

                if (numRemoved > 0)
                {
                    Database.KeyDelete(keys);
                    numRemoved += RemoveDependingEntries(keys.Select(x => NormalizeKey(x)).ToArray());
                }

                PostMessage("removebypattern^" + pattern);
            });

            return numRemoved;
        }

        public async Task<long> RemoveByPatternAsync(string pattern)
        {
            int numRemoved = 0;

            await RedisActionAsync(true, async () =>
            {
                var keys = await _multiplexer.GetKeysAsync(BuildCacheKey(pattern)).Select(x => (RedisKey)x).ToArrayAsync();
                numRemoved = keys.Length;

                if (numRemoved > 0)
                {
                    await Database.KeyDeleteAsync(keys);
                    numRemoved += await RemoveDependingEntriesAsync(keys.Select(x => NormalizeKey(x)).ToArray());
                }

                await PostMessageAsync("removebypattern^" + pattern);
            });

            return numRemoved;
        }

        #region Dependent Entries

        private void EnlistDependencyKeys(string key, IEnumerable<string> dependencies)
        {
            if (dependencies == null || !dependencies.Any())
                return;

            // INFO: we must evict "key" when ANY of the "dependencies" change,
            // therefore we create a hashset lookup for each dependency and add "key" to the set.

            foreach (var lookupKey in dependencies.Select(x => BuildDependencyLookupKey(x)).ToArray())
            {
                Database.SetAdd(lookupKey, key);
                //Debug.WriteLine("REDIS: '{0}' depends on '{1}'".FormatInvariant(key, lookupKey));
            }
        }

        private async Task EnlistDependencyKeysAsync(string key, IEnumerable<string> dependencies)
        {
            if (dependencies == null || !dependencies.Any())
                return;
            
            // INFO: we must evict "key" when ANY of the "dependencies" change,
            // therefore we create a hashset lookup for each dependency and add "key" to the set.

            foreach (var lookupKey in dependencies.Select(x => BuildDependencyLookupKey(x)).ToArray())
            {
                await Database.SetAddAsync(lookupKey, key).ConfigureAwait(false);
            }
        }

        private (string lookupKey, IEnumerable<string> keys) GetDependingKeys(string key)
        {
            var lookupKey = BuildDependencyLookupKey(key);

            var set = Database.SetMembers(lookupKey);
            if (set != null && set.Length > 0)
            {
                var keys = set.Select(x => (string)x).Where(x => x.HasValue());
                return (lookupKey, keys);
            }

            return (lookupKey, Enumerable.Empty<string>());
        }

        private async ValueTask<(string lookupKey, IEnumerable<string> keys)> GetDependingKeysAsync(string key)
        {
            var lookupKey = BuildDependencyLookupKey(key);

            var set = await Database.SetMembersAsync(lookupKey).ConfigureAwait(false);
            if (set != null && set.Length > 0)
            {
                var keys = set.Select(x => (string)x).Where(x => x.HasValue());
                return (lookupKey, keys);
            }

            return (lookupKey, Enumerable.Empty<string>());
        }

        private int RemoveDependingEntries(IEnumerable<string> keys)
        {
            var dependingKeys = new HashSet<string>();
            var lookupKeys = new HashSet<RedisKey>();

            long numDeleted = 0;

            // Combine all depending entry keys for each passed source key
            foreach (var key in keys.Distinct().ToArray())
            {
                var keys2 = GetDependingKeys(key);
                if (keys2.keys.Any())
                {
                    dependingKeys.AddRange(keys2.keys);
                    lookupKeys.Add(keys2.lookupKey);
                }
            }

            if (dependingKeys.Any())
            {
                // Delete all depending entries in one go
                numDeleted += Database.KeyDelete(dependingKeys.Select(x => (RedisKey)BuildCacheKey(x)).ToArray());

                //foreach (var k in dependingKeys)
                //{
                //	Debug.WriteLine("REDIS: remove depending key '{0}'. Lookups: {1}".FormatInvariant(k, string.Join(", ", keys)));
                //}

                if (numDeleted > 0)
                {
                    // Recursive call
                    numDeleted += RemoveDependingEntries(dependingKeys);
                }
            }

            // Finally delete all lookup sets
            if (lookupKeys.Any())
            {
                Database.KeyDelete(lookupKeys.ToArray());
            }

            return (int)numDeleted;
        }

        private async Task<int> RemoveDependingEntriesAsync(IEnumerable<string> keys)
        {
            var dependingKeys = new HashSet<string>();
            var lookupKeys = new HashSet<RedisKey>();

            long numDeleted = 0;

            // Combine all depending entry keys for each passed source key
            foreach (var key in keys.Distinct().ToArray())
            {
                var keys2 = await GetDependingKeysAsync(key).ConfigureAwait(false);
                if (keys2.keys.Any())
                {
                    dependingKeys.AddRange(keys2.keys);
                    lookupKeys.Add(keys2.lookupKey);
                }
            }

            if (dependingKeys.Any())
            {
                // Delete all depending entries in one go
                numDeleted += await Database.KeyDeleteAsync(dependingKeys.Select(x => (RedisKey)BuildCacheKey(x)).ToArray()).ConfigureAwait(false);
                if (numDeleted > 0)
                {
                    // Recursive call
                    numDeleted += await RemoveDependingEntriesAsync(dependingKeys).ConfigureAwait(false);
                }
            }

            // Finally delete all lookup sets
            if (lookupKeys.Any())
            {
                await Database.KeyDeleteAsync(lookupKeys.ToArray()).ConfigureAwait(false);
            }

            return (int)numDeleted;
        }

        internal string BuildDependencyLookupKey(string key)
        {
            return BuildCacheKey("deplookup:" + key);
        }

        #endregion

        #region Utilities

        private bool RedisAction(bool condition, Action action)
        {
            if (condition && CheckLicense() && CheckConnection())
            {
                action();
                return true;
            }

            return false;
        }

        private async Task<bool> RedisActionAsync(bool condition, Func<Task> action)
        {
            if (condition && CheckLicense() && CheckConnection())
            {
                await action().ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private void PostMessage(string message)
        {
            _messageBus.Publish("cache", message);
        }

        private Task PostMessageAsync(string message)
        {
            return _messageBus.PublishAsync("cache", message);
        }

        private bool CheckLicense()
        {
            return true;
        }

        private bool CheckConnection()
        {
            return true;
        }

        private string NormalizeKey(string redisKey)
        {
            return redisKey[_keyPrefix.Length..];
        }

        internal string BuildCacheKey(string key)
        {
            return RedisUtility.BuildScopedKey(_cachePrefix + key);
        }

        #endregion
    }
}