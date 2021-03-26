using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Stores
{
    [Important]
    public class StoreContext : DbSaveHook<BaseEntity>, IStoreContext
    {
        internal const string OverriddenStoreIdKey = "OverriddenStoreId";
        const string CacheKey = "stores:all";

        private readonly ICacheFactory _cacheFactory;
        private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;

        private Store _currentStore;

        public StoreContext(
            ICacheFactory cacheFactory,
            IDbContextFactory<SmartDbContext> dbContextFactory,
            IHttpContextAccessor httpContextAccessor,
            IActionContextAccessor actionContextAccessor)
        {
            _cacheFactory = cacheFactory;
            _dbContextFactory = dbContextFactory;
            _httpContextAccessor = httpContextAccessor;
            _actionContextAccessor = actionContextAccessor;
        }

        #region Hook

        protected override HookResult OnDeleting(BaseEntity entity, IHookedEntity entry)
        {
            if (entry.Entity is Store)
            {
                if (GetCachedStores().Stores.Count == 1)
                {
                    entry.State = Smartstore.Data.EntityState.Unchanged;
                    throw new InvalidOperationException("Cannot delete the only configured store.");
                }

                return HookResult.Ok;
            }
            else
            {
                return HookResult.Void;
            }
        }

        public override HookResult OnAfterSave(IHookedEntity entry)
        {
            if (entry.Entity is Store || entry.Entity is Currency)
            {
                _cacheFactory.GetHybridCache().Remove(CacheKey);
                return HookResult.Ok;
            }
            else
            {
                return HookResult.Void;
            }
        }

        #endregion

        public virtual Store CurrentStore
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

        public StoreEntityCache GetCachedStores()
        {
            return _cacheFactory.GetMemoryCache().Get(CacheKey, (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));
                
                var entry = new StoreEntityCache();

                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.ChangeTracker.LazyLoadingEnabled = false;

                    var allStores = db.Stores
                        .AsNoTracking()
                        .AsNoCaching()
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
            try
            {
                var cookie = _httpContextAccessor.HttpContext.GetPreviewModeFromCookie();
                if (cookie != null)
                {
                    return cookie[OverriddenStoreIdKey].ToString().Convert<int?>();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void SetPreviewStore(int? storeId)
        {
            _httpContextAccessor.HttpContext.SetPreviewModeValueInCookie(OverriddenStoreIdKey, storeId.HasValue ? storeId.Value.ToString() : null);
            _currentStore = null;
        }
    }
}
