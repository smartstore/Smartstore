using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;
using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Services
{
    public partial class WebApiService : IWebApiService
    {
        internal const string StateKey = "smartstore.webapi:state";
        internal const string AuthorizedCustomersKey = "smartstore.webapi:authorizedcustomers";

        private readonly ICacheManager _cache;
        private readonly IServiceProvider _serviceProvider;

        public WebApiService(ICacheManager cache, IServiceProvider serviceProvider)
        {
            _cache = cache;
            _serviceProvider = serviceProvider;
        }

        public WebApiState GetState()
        {
            return _cache.Get(StateKey, (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(30));

                var descriptor = _serviceProvider.GetService<IModuleCatalog>().GetModuleByName(Module.SystemName);
                var settings = _serviceProvider.GetService<WebApiSettings>();

                var state = new WebApiState
                {
                    IsActive = descriptor?.IsInstalled() ?? false,
                    ModuleVersion = descriptor?.Version?.ToString()?.NullEmpty() ?? "1.0",
                    LogUnauthorized = settings.LogUnauthorized,
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
            var result = await _cache.GetAsync(StateKey, async () =>
            {
                var db = _serviceProvider.GetService<SmartDbContext>();

                var attributesQuery =
                    from a in db.GenericAttributes
                    join c in db.Customers on a.EntityId equals c.Id
                    where !c.Deleted && c.Active && a.KeyGroup == "Customer" && a.Key == "WebApiUserData"
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
