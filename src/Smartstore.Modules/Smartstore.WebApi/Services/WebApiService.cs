using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Threading;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api
{
    public partial class WebApiService : IWebApiService
    {
        // {0} = StoreId
        internal const string StateKey = "smartstore.webapi:state-{0}";
        internal const string StatePatternKey = "smartstore.webapi:state-*";

        internal const string UsersKey = "smartstore.webapi:users";
        internal const string AttributeUserDataKey = "WebApiUserData";

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly IMemoryCache _memCache;
        private readonly ISettingFactory _settingFactory;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly CancellationToken _appShutdownCancellationToken;

        public WebApiService(
            SmartDbContext db,
            IStoreContext storeContext,
            ICacheManager cache,
            IMemoryCache memCache,
            ISettingFactory settingFactory,
            IModuleCatalog moduleCatalog,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _db = db;
            _storeContext = storeContext;
            _cache = cache;
            _memCache = memCache;
            _settingFactory = settingFactory;
            _moduleCatalog = moduleCatalog;
            _appShutdownCancellationToken = hostApplicationLifetime.ApplicationStopping;
        }

        /// <summary>
        /// Creates a pair of of cryptographic random numbers as a hex string.
        /// </summary>
        /// <param name="key1">First created key.</param>
        /// <param name="key2">Second created key.</param>
        /// <param name="length">The length of the keys to be generated.</param>
        /// <returns><c>true</c> succeeded otherwise <c>false</c>.</returns>
        public static bool CreateKeys(out string key1, out string key2, int length = 32)
        {
            key1 = key2 = null;

            using (var rng = RandomNumberGenerator.Create())
            {
                for (var i = 0; i < 9999; i++)
                {
                    var data1 = new byte[length];
                    var data2 = new byte[length];

                    rng.GetNonZeroBytes(data1);
                    rng.GetNonZeroBytes(data2);

                    key1 = data1.ToHexString(false, length);
                    key2 = data2.ToHexString(false, length);

                    if (key1 != key2)
                    {
                        break;
                    }
                }
            }

            return key1.HasValue() && key2.HasValue() && key1 != key2;
        }

        public WebApiState GetState(int? storeId = null)
        {
            storeId ??= _storeContext.CurrentStore.Id;

            return _cache.Get(StateKey.FormatInvariant(storeId.Value), (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(30));

                var settings = _settingFactory.LoadSettings<WebApiSettings>(storeId.Value);
                var descriptor = _moduleCatalog.GetModuleByName(Module.SystemName);

                var state = new WebApiState
                {
                    IsActive = (descriptor?.IsInstalled() ?? false) && settings.IsActive,
                    ModuleVersion = descriptor?.Version?.ToString()?.NullEmpty() ?? "1.0",
                    MaxTop = settings.MaxTop,
                    MaxExpansionDepth = settings.MaxExpansionDepth
                };

                return state;
            });
        }

        public async Task<Dictionary<string, WebApiUser>> GetApiUsersAsync()
        {
            if (_memCache.TryGetValue(UsersKey, out object cachedUsers))
            {
                return (Dictionary<string, WebApiUser>)cachedUsers;
            }

            var attributesQuery =
                from a in _db.GenericAttributes
                join c in _db.Customers on a.EntityId equals c.Id
                where !c.Deleted && c.Active && a.KeyGroup == nameof(Customer) && a.Key == AttributeUserDataKey
                select new
                {
                    a.Id,
                    a.EntityId,
                    a.Value
                };

            var attributes = await attributesQuery.ToListAsync();
            var processedCustomerIds = new HashSet<int>();

            var entries = attributes
                .Where(x => x.Value.HasValue())
                .Select(x =>
                {
                    if (!processedCustomerIds.Contains(x.EntityId))
                    {
                        string[] arr = x.Value.SplitSafe('¶').ToArray();

                        if (arr.Length > 2)
                        {
                            var entry = new WebApiUser
                            {
                                GenericAttributeId = x.Id,
                                CustomerId = x.EntityId,
                                Enabled = bool.Parse(arr[0]),
                                PublicKey = arr[1],
                                SecretKey = arr[2],
                                LastRequest = arr.Length > 3
                                    ? DateTime.ParseExact(arr[3], "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                                    : null
                            };

                            if (entry.IsValid)
                            {
                                processedCustomerIds.Add(x.EntityId);
                                return entry;
                            }
                        }
                    }

                    return null;
                })
                .Where(x => x != null)
                .ToList();

            var users = entries.ToDictionarySafe(x => x.PublicKey, StringComparer.OrdinalIgnoreCase);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.NeverRemove)
                .AddExpirationToken(new CancellationChangeToken(_appShutdownCancellationToken))
                .RegisterPostEvictionCallback(OnPostEvictionCallback);

            return _memCache.Set(UsersKey, users, cacheEntryOptions);
        }

        public void ClearApiUserCache()
        {
            _memCache.Remove(UsersKey);
        }

        private static void OnPostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (reason == EvictionReason.Removed || reason == EvictionReason.TokenExpired)
            {
                ContextState.StartAsyncFlow();

                var apiUserStore = EngineContext.Current.Application.Services.Resolve<IApiUserStore>();
                apiUserStore.SaveApiUsers(value as Dictionary<string, WebApiUser>);
            }
        }
    }
}
