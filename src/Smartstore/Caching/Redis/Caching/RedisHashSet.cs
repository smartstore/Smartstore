using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Caching;
using StackExchange.Redis;

namespace Smartstore.Redis.Caching
{
    internal partial class RedisHashSet : ISet
    {
        private readonly RedisKey _redisKey;
        private readonly RedisCacheStore _cache;
        private readonly IDatabase _database;

        public RedisHashSet(RedisKey redisKey, RedisCacheStore cache)
        {
            _redisKey = redisKey;
            _cache = cache;
            _database = cache.Database;
        }

        public int Count 
            => (int)_database.SetLength(_redisKey);

        public bool Add(string item)
            => _database.SetAdd(_redisKey, item);

        public Task<bool> AddAsync(string item)
            => _database.SetAddAsync(_redisKey, item);

        public void AddRange(IEnumerable<string> items)
            => _database.SetAdd(_redisKey, Array.ConvertAll(items?.ToArray() ?? Array.Empty<string>(), x => (RedisValue)x));

        public Task AddRangeAsync(IEnumerable<string> items)
            => _database.SetAddAsync(_redisKey, Array.ConvertAll(items?.ToArray() ?? Array.Empty<string>(), x => (RedisValue)x));

        public void Clear()
            => _database.KeyDelete(_redisKey);

        public Task ClearAsync()
            => _database.KeyDeleteAsync(_redisKey);

        public bool Contains(string item)
            => _database.SetContains(_redisKey, item);

        public Task<bool> ContainsAsync(string item)
            => _database.SetContainsAsync(_redisKey, item);

        public bool Remove(string item)
            => _database.SetRemove(_redisKey, item);

        public Task<bool> RemoveAsync(string item)
            => _database.SetRemoveAsync(_redisKey, item);

        public bool Move(string destinationKey, string item)
            => _database.SetMove(_redisKey, _cache.BuildCacheKey(destinationKey), item);

        public Task<bool> MoveAsync(string destinationKey, string item)
            => _database.SetMoveAsync(_redisKey, _cache.BuildCacheKey(destinationKey), item);

        public long UnionWith(params string[] keys)
            => Combine(SetOperation.Union, keys);

        public Task<long> UnionWithAsync(params string[] keys)
            => CombineAsync(SetOperation.Union, keys);

        public long IntersectWith(params string[] keys)
            => Combine(SetOperation.Intersect, keys);

        public Task<long> IntersectWithAsync(params string[] keys)
            => CombineAsync(SetOperation.Intersect, keys);

        public long ExceptWith(params string[] keys)
            => Combine(SetOperation.Difference, keys);

        public Task<long> ExceptWithAsync(params string[] keys)
            => CombineAsync(SetOperation.Difference, keys);

        private long Combine(SetOperation op, params string[] keys)
        {
            if (keys.Length == 0)
                return 0;

            var targetKeys = Array.ConvertAll(keys, x => (RedisKey)_cache.BuildCacheKey(x));
            return _database.SetCombineAndStore(op, _redisKey, targetKeys);
        }

        private Task<long> CombineAsync(SetOperation op, params string[] keys)
        {
            if (keys.Length == 0)
                return Task.FromResult((long)0);

            var targetKeys = Array.ConvertAll(keys, x => (RedisKey)_cache.BuildCacheKey(x));
            return _database.SetCombineAndStoreAsync(op, _redisKey, targetKeys);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            var members = _database.SetMembers(_redisKey);

            if (members != null && members.Length > 0)
            {
                var set = members.Select(x => (string)x);
                return set.GetEnumerator();
            }

            return Enumerable.Empty<string>().GetEnumerator();
        }
    }
}
