using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Core.Common.Services
{
    public partial class CountryService : AsyncDbSaveHook<Country>, ICountryService
    {
        private const string COUNTRIES_ALL_KEY = "SmartStore.country.all-{0}";
        private const string COUNTRIES_BILLING_KEY = "SmartStore.country.billing-{0}";
        private const string COUNTRIES_SHIPPING_KEY = "SmartStore.country.shipping-{0}";
        private const string COUNTRIES_PATTERN_KEY = "SmartStore.country.*";

        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICacheManager _cache;

        public CountryService(
            SmartDbContext db,
            ICommonServices services, 
            ICacheManager cache)
        {
            _db = db;
            _services = services;
            _cache = cache;
        }

        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Hook 

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            await _cache.RemoveAsync(COUNTRIES_PATTERN_KEY);

            return HookResult.Ok;
        }

        #endregion

        public virtual async Task<IList<Country>> GetCountriesAsync(bool showHidden = false)
        {
            string key = string.Format(COUNTRIES_ALL_KEY, showHidden);
            return await _services.RequestCache.GetAsync(key, () =>
            {
                var query = _db.Countries.AsNoTracking();

                if (!showHidden)
                    query = query.Where(c => c.Published);

                if (!showHidden && !QuerySettings.IgnoreMultiStore)
                {
                    var currentStoreId = _services.StoreContext.CurrentStore.Id;
                    query = from c in query
                            join sc in _db.StoreMappings.AsNoTracking()
                            on new { c1 = c.Id, c2 = "Country" } equals new { c1 = sc.EntityId, c2 = sc.EntityName } into c_sm
                            from sc in c_sm.DefaultIfEmpty()
                            where !c.LimitedToStores || currentStoreId == sc.StoreId
                            select c;

                    // TODO: (core) Does not work with efcore5 anymore??
                    //query = from c in query
                    //        group c by c.Id into cGroup
                    //        orderby cGroup.Key
                    //        select cGroup.FirstOrDefault();
                }
                
                query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);

                return query.ToListAsync();
            });
        }

        public virtual async Task<IList<Country>> GetCountriesForBillingAsync(bool showHidden = false)
        {
            string key = string.Format(COUNTRIES_BILLING_KEY, showHidden);
            return await _services.RequestCache.GetAsync(key, async () =>
            {
                var allCountries = await GetCountriesAsync(showHidden);
                return allCountries.Where(x => x.AllowsBilling).ToList(); 
            });
        }

        public virtual async Task<IList<Country>> GetCountriesForShippingAsync(bool showHidden = false)
        {
            string key = string.Format(COUNTRIES_SHIPPING_KEY, showHidden);
            return await _services.RequestCache.GetAsync(key, async () =>
            {
                var allCountries = await GetCountriesAsync(showHidden);
                return allCountries.Where(x => x.AllowsShipping).ToList();
            });
        }
    }
}