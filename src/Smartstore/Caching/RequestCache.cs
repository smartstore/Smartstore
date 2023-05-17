using Microsoft.AspNetCore.Http;

namespace Smartstore.Caching
{
    public class RequestCache : Disposable, IRequestCache
    {
        const string RegionName = "_SmartstoreApp";

        private Dictionary<object, object> _localDictionary;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestCache(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public IDictionary<object, object> Items
        {
            get => GetItemsDictionary();
        }

        protected IDictionary<object, object> GetItemsDictionary()
        {
            var httpItems = _httpContextAccessor.HttpContext?.Items;

            if (httpItems != null)
            {
                if (!httpItems.TryGetValue(RegionName, out var items))
                {
                    httpItems[RegionName] = items = new Dictionary<object, object>();
                }

                return (IDictionary<object, object>)items;
            }
            else
            {
                return _localDictionary ??= new();
            }
        }

        public T Get<T>(object key)
        {
            return Get<T>(key, null);
        }

        public T Get<T>(object key, Func<T> acquirer)
        {
            var items = GetItemsDictionary();
            
            if (items.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            if (acquirer != null)
            {
                value = acquirer();
                items[key] = value;
                return (T)value;
            }

            return default;
        }

        public async Task<T> GetAsync<T>(object key, Func<Task<T>> acquirer)
        {
            var items = GetItemsDictionary();

            if (items.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            if (acquirer != null)
            {
                value = await acquirer();
                items[key] = value;
                return (T)value;
            }

            return default;
        }

        public void Put(object key, object value)
        {
            GetItemsDictionary()[key] = value;
        }

        public void Clear()
        {
            GetItemsDictionary().Clear();
        }

        public bool Contains(object key)
        {
            return GetItemsDictionary().ContainsKey(key);
        }

        public void Remove(object key)
        {
            GetItemsDictionary().Remove(key);
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                Clear();
            }
        }
    }
}
