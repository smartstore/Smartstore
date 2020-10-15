using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Smartstore.Caching;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Stores
{
    public class StoreContext : DbSaveHook<BaseEntity>, IStoreContext
    {
        public class StoreEntityCache
        {
            public IDictionary<int, Store> Stores { get; set; }
            public IDictionary<string, int> HostMap { get; set; }
            public int PrimaryStoreId { get; set; }

            internal Store GetPrimaryStore()
            {
                return Stores.Get(PrimaryStoreId);
            }

            internal Store GetStoreById(int id)
            {
                return Stores.Get(id);
            }

            internal Store GetStoreByHostName(string host)
            {
                if (!string.IsNullOrEmpty(host) && HostMap.TryGetValue(host, out var id))
                {
                    return Stores.Get(id);
                }

                return null;
            }
        }

        internal const string OverriddenStoreIdKey = "OverriddenStoreId";
        const string CacheKey = "stores:all";

        private readonly ICacheManager _cache;
        private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;

        private Store _currentStore;

        public StoreContext(
            ICacheManager cache,
            IDbContextFactory<SmartDbContext> dbContextFactory,
            IHttpContextAccessor httpContextAccessor,
            IActionContextAccessor actionContextAccessor)
        {
            _cache = cache;
            _dbContextFactory = dbContextFactory;
            _httpContextAccessor = httpContextAccessor;
            _actionContextAccessor = actionContextAccessor;
        }

        public int? GetRequestStore()
        {
            return _actionContextAccessor.ActionContext?.RouteData?.DataTokens?.Get(OverriddenStoreIdKey)?.Convert<int?>();
        }

        public void SetRequestStore(int? storeId)
        {
            var dataTokens = _actionContextAccessor.ActionContext?.RouteData?.DataTokens;

            if (dataTokens != null)
            {
                if (storeId.GetValueOrDefault() > 0)
                {
                    dataTokens[OverriddenStoreIdKey] = storeId.Value;
                }
                else if (dataTokens.ContainsKey(OverriddenStoreIdKey))
                {
                    dataTokens.Remove(OverriddenStoreIdKey);
                }

                _currentStore = null;
            }
        }

        public int? GetPreviewStore()
        {
            // TODO: (core) Implement GetPreviewModeCookie() extension
            //var cookie = _httpContext.Value.GetPreviewModeCookie(false);
            //if (cookie != null)
            //{
            //    var value = cookie.Values[OverriddenStoreIdKey];
            //    if (value.HasValue())
            //    {
            //        return value.ToInt();
            //    }
            //}

            return null;
        }

        public void SetPreviewStore(int? storeId)
        {
            // TODO: (core) Implement GetPreviewModeCookie() extension
            //_httpContext.Value.SetPreviewModeValue(OverriddenStoreIdKey, storeId.HasValue ? storeId.Value.ToString() : null);
            _currentStore = null;
        }

        public Store CurrentStore
        {
            get
            {
                if (_currentStore == null)
                {
                    var cachedStores = GetCachedStores();

                    int? storeOverride = GetRequestStore() ?? GetPreviewStore();
                    if (storeOverride.HasValue)
                    {
                        // The store to be used can be overwritten on request basis (e.g. for theme preview, editing etc.)
                        _currentStore = cachedStores.GetStoreById(storeOverride.Value);
                    }

                    if (_currentStore == null)
                    {
                        // Try to determine the current store by HTTP_HOST
                        var hostName = _httpContextAccessor.HttpContext?.Request?.Host.Value;

                        _currentStore =
                            // Try to resolve the current store by HTTP_HOST
                            cachedStores.GetStoreByHostName(hostName) ??
                            // Then resolve primary store
                            cachedStores.GetPrimaryStore() ??
                            // No way
                            throw new Exception("No store could be loaded.");
                    }
                }

                return _currentStore;
            }
            set => _currentStore = value;
        }

        public int CurrentStoreIdIfMultiStoreMode => GetCachedStores().Stores.Count <= 1 ? 0 : CurrentStore.Id;

        protected StoreEntityCache GetCachedStores()
        {
            return _cache.Get(CacheKey, () =>
            {
                var entry = new StoreEntityCache();

                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.ChangeTracker.LazyLoadingEnabled = false;
                    
                    var allStores = db.Stores
                        .AsNoTracking()
                        .Include(x => x.PrimaryStoreCurrency)
                        .Include(x => x.PrimaryExchangeRateCurrency)
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.Name)
                        .ToList();

                    entry.Stores = allStores.ToDictionary(x => x.Id);
                    entry.HostMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var store in allStores)
                    {
                        var hostValues = store.ParseHostValues();
                        foreach (var host in hostValues)
                        {
                            entry.HostMap[host] = store.Id;
                        }
                    }

                    if (allStores.Count > 0)
                    {
                        entry.PrimaryStoreId = allStores.FirstOrDefault().Id;
                    }
                }

                return entry;
            }, allowRecursion: true);
        }

        public override void OnAfterSave(IHookedEntity entry)
        {
            if (entry.Entity is Store || entry.Entity is Currency)
            {
                _cache.Remove(CacheKey);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
