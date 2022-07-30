using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Smartstore.Utilities;

namespace Smartstore.Caching
{
    public class RequestCache : Disposable, IRequestCache
    {
        const string RegionName = "Smartstore:";

        private Dictionary<object, object> _localDictionary;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestCache(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public T Get<T>(string key)
        {
            return Get<T>(key, null);
        }

        public T Get<T>(string key, Func<T> acquirer)
        {
            var items = GetItems();

            key = BuildKey(key);

            if (items.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            if (acquirer != null)
            {
                value = acquirer();
                items.Add(key, value);
                return (T)value;
            }

            return default;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquirer)
        {
            var items = GetItems();

            key = BuildKey(key);

            if (items.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            if (acquirer != null)
            {
                value = await acquirer();
                items.Add(key, value);
                return (T)value;
            }

            return default;
        }

        public void Put(string key, object value)
        {
            GetItems()[BuildKey(key)] = value;
        }

        public void Clear()
        {
            RemoveByPattern("*");
        }

        public bool Contains(string key)
        {
            return GetItems().ContainsKey(BuildKey(key));
        }

        public void Remove(string key)
        {
            var items = GetItems();
            key = BuildKey(key);

            if (items.ContainsKey(key))
            {
                items.Remove(key);
            }
        }

        public void RemoveByPattern(string pattern)
        {
            var items = GetItems();

            var keysToRemove = Keys(pattern).ToArray();

            foreach (string key in keysToRemove)
            {
                items.Remove(BuildKey(key));
            }
        }

        protected IDictionary<object, object> GetItems()
        {
            return _httpContextAccessor?.HttpContext?.Items ?? (_localDictionary ??= new());
        }

        public IEnumerable<string> Keys(string pattern)
        {
            var items = GetItems();

            if (items.Count == 0)
                yield break;

            var prefixLen = RegionName.Length;

            pattern = pattern.NullEmpty() ?? "*";
            var wildcard = new Wildcard(pattern, RegexOptions.IgnoreCase);

            foreach (var kvp in items)
            {
                if (kvp.Key is string key)
                {
                    if (key.StartsWith(RegionName))
                    {
                        key = key[prefixLen..];
                        if (pattern == "*" || wildcard.IsMatch(key))
                        {
                            yield return key;
                        }
                    }
                }
            }
        }

        private static string BuildKey(string key)
        {
            return RegionName + key.EmptyNull();
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                Clear();
        }
    }
}
