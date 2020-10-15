using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Collections;

namespace Smartstore.Caching
{
    public class NullCache : ICacheManager
    {
        public static ICacheManager Instance => new NullCache();

        public T Get<T>(string key, bool independent = false) 
            => default;

        public Task<T> GetAsync<T>(string key, bool independent = false) 
            => Task.FromResult<T>(default);

        public T Get<T>(string key, Func<CacheEntryOptions, T> acquirer, bool independent = false, bool allowRecursion = false) 
            => acquirer == null ? default : acquirer(new CacheEntryOptions());

        public Task<T> GetAsync<T>(string key, Func<CacheEntryOptions, Task<T>> acquirer, bool independent = false, bool allowRecursion = false)
            => acquirer == null ? default : acquirer(new CacheEntryOptions());

        public ISet GetHashSet(string key, Func<IEnumerable<string>> acquirer = null)
            => new MemorySet(null);

        public Task<ISet> GetHashSetAsync(string key, Func<Task<IEnumerable<string>>> acquirer = null)
            => Task.FromResult<ISet>(new MemorySet(null));

        public void Put(string key, object value, CacheEntryOptions options = null)
            { }

        public Task PutAsync(string key, object value, CacheEntryOptions options = null)
            => Task.CompletedTask;

        public bool Contains(string key)
            => false;

        public Task<bool> ContainsAsync(string key)
            => Task.FromResult(false);

        public void Remove(string key)
            { }

        public Task RemoveAsync(string key)
            => Task.CompletedTask;

        public IEnumerable<string> Keys(string pattern = "*")
            => Array.Empty<string>();

        public IAsyncEnumerable<string> KeysAsync(string pattern = "*")
            => Keys(pattern).ToAsyncEnumerable();

        public long RemoveByPattern(string pattern)
            => 0;

        public Task<long> RemoveByPatternAsync(string pattern)
            => Task.FromResult((long)0);

        public void Clear()
            { }

        public Task ClearAsync()
            => Task.CompletedTask;
    }
}