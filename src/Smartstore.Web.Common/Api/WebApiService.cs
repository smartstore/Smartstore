using System.Globalization;
using System.Security.Cryptography;
using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Api
{
    public partial class WebApiService : IWebApiService
    {
        // {0} = StoreId
        public const string StateKey = "smartstore.webapi:state-{0}";
        public const string StatePatternKey = "smartstore.webapi:state-*";

        public const string UsersKey = "smartstore.webapi:users";
        public const string AttributeUserDataKey = "WebApiUserData";

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly ISettingFactory _settingFactory;
        private readonly IModuleCatalog _moduleCatalog;

        public WebApiService(
            SmartDbContext db,
            IStoreContext storeContext,
            ICacheManager cache, 
            ISettingFactory settingFactory,
            IModuleCatalog moduleCatalog)
        {
            _db = db;
            _storeContext = storeContext;
            _cache = cache;
            _settingFactory = settingFactory;
            _moduleCatalog = moduleCatalog;
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
                var descriptor = _moduleCatalog.GetModuleByName("Smartstore.WebApi");

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
            // TODO: (mg) (core) is CacheItemRemovedCallback gone forever? Find replacement for the CacheItemRemovedCallback logic
            // for non-removable cache entries? We have to store\update data as GenericAttribute.
            var result = await _cache.GetAsync(UsersKey, async () =>
            {
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
                                    SecretKey = arr[2]
                                };

                                if (arr.Length > 3)
                                {
                                    entry.LastRequest = DateTime.ParseExact(arr[3], "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                                }

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

                return entries.ToDictionarySafe(x => x.PublicKey, StringComparer.OrdinalIgnoreCase);
            });

            return result;
        }
    }
}
