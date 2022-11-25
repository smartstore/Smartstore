using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    public class LanguageStub
    {
        public int Id { get; set; }
        public string UniqueSeoCode { get; set; }
    }

    public partial class LanguageService : AsyncDbSaveHook<BaseEntity>, ILanguageService
    {
        private const string STORE_LANGUAGE_MAP_KEY = "storelangmap";

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly ISettingFactory _settingFactory;
        private readonly Lazy<LocalizationSettings> _localizationSettings;

        public LanguageService(
            ICacheManager cache,
            SmartDbContext db,
            ISettingFactory settingFactory,
            Lazy<LocalizationSettings> localizationSettings,
            IStoreContext storeContext)
        {
            _cache = cache;
            _db = db;
            _settingFactory = settingFactory;
            _localizationSettings = localizationSettings;
            _storeContext = storeContext;
        }

        #region Hook

        protected override async Task<HookResult> OnDeletingAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity is not Language language)
            {
                return HookResult.Void;
            }

            // Update default admin area language (if required)
            var localizationSettings = _localizationSettings.Value;
            if (localizationSettings.DefaultAdminLanguageId == language.Id)
            {
                var allLanguages = await GetAllLanguagesAsync();
                foreach (var activeLanguage in allLanguages)
                {
                    if (activeLanguage.Id != language.Id)
                    {
                        localizationSettings.DefaultAdminLanguageId = activeLanguage.Id;
                        await _settingFactory.SaveSettingsAsync(localizationSettings);
                        break;
                    }
                }
            }

            return HookResult.Ok;
        }

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.EntityType != typeof(Store) && entry.EntityType != typeof(Language))
                return HookResult.Void;

            await _cache.RemoveAsync(STORE_LANGUAGE_MAP_KEY);

            return HookResult.Ok;
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedLanguageIds = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<Language>()
                .ToDistinctArray(x => x.Id);

            if (deletedLanguageIds.Length > 0)
            {
                await _db.GenericAttributes
                    .Where(x => deletedLanguageIds.Contains(x.EntityId) && x.KeyGroup == nameof(Language))
                    .ExecuteDeleteAsync(cancelToken);
            }
        }

        #endregion

        #region ILanguageService

        public virtual bool IsMultiLanguageEnvironment(int storeId = 0)
        {
            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = GetStoreLanguageMap();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].Count > 1;
            }

            return false;
        }

        public virtual List<Language> GetAllLanguages(bool includeHidden = false, int storeId = 0, bool tracked = false)
        {
            return _db.Languages
                .ApplyTracking(tracked)
                .ApplyStandardFilter(includeHidden, storeId)
                .ToList();
        }

        public virtual async Task<List<Language>> GetAllLanguagesAsync(bool includeHidden = false, int storeId = 0, bool tracked = false)
        {
            return await _db.Languages
                .ApplyTracking(tracked)
                .ApplyStandardFilter(includeHidden, storeId)
                .ToListAsync();
        }

        public virtual bool IsPublishedLanguage(int languageId, int storeId = 0)
        {
            if (languageId <= 0)
                return false;

            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = GetStoreLanguageMap();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].Any(x => x.Id == languageId);
            }

            return false;
        }

        public virtual async Task<bool> IsPublishedLanguageAsync(int languageId, int storeId = 0)
        {
            if (languageId <= 0)
                return false;

            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = await GetStoreLanguageMapAsync();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].Any(x => x.Id == languageId);
            }

            return false;
        }

        public virtual bool IsPublishedLanguage(string seoCode, int storeId = 0)
        {
            if (seoCode.IsEmpty())
                return false;

            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = GetStoreLanguageMap();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].Any(x => x.UniqueSeoCode == seoCode);
            }

            return false;
        }

        public virtual async Task<bool> IsPublishedLanguageAsync(string seoCode, int storeId = 0)
        {
            if (seoCode.IsEmpty())
                return false;

            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = await GetStoreLanguageMapAsync();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].Any(x => x.UniqueSeoCode == seoCode);
            }

            return false;
        }

        public virtual string GetMasterLanguageSeoCode(int storeId = 0)
        {
            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = GetStoreLanguageMap();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].FirstOrDefault().UniqueSeoCode;
            }

            return null;
        }

        public virtual async Task<string> GetMasterLanguageSeoCodeAsync(int storeId = 0)
        {
            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = await GetStoreLanguageMapAsync();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].FirstOrDefault().UniqueSeoCode;
            }

            return null;
        }

        public virtual int GetMasterLanguageId(int storeId = 0)
        {
            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = GetStoreLanguageMap();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].FirstOrDefault().Id;
            }

            return 0;
        }

        public virtual async Task<int> GetMasterLanguageIdAsync(int storeId = 0)
        {
            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = await GetStoreLanguageMapAsync();
            if (map.ContainsKey(storeId))
            {
                return map[storeId].FirstOrDefault().Id;
            }

            return 0;
        }

        /// <summary>
        /// Gets a map of active/published store languages
        /// </summary>
        /// <returns>A map of store languages where key is the store id and values are tuples of language ids and seo codes</returns>
        protected Multimap<int, LanguageStub> GetStoreLanguageMap()
        {
            return GetStoreLanguageMapInternal(false).Await();
        }

        /// <summary>
        /// Gets a map of active/published store languages
        /// </summary>
        /// <returns>A map of store languages where key is the store id and values are tuples of language ids and seo codes</returns>
        protected Task<Multimap<int, LanguageStub>> GetStoreLanguageMapAsync()
        {
            return GetStoreLanguageMapInternal(true);
        }

        private async Task<Multimap<int, LanguageStub>> GetStoreLanguageMapInternal(bool async)
        {
            var result = await _cache.GetAsync(STORE_LANGUAGE_MAP_KEY, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var map = new Multimap<int, LanguageStub>();

                var allStores = _storeContext.GetAllStores();
                foreach (var store in allStores)
                {
                    var languages = async ? await GetAllLanguagesAsync(false, store.Id) : GetAllLanguages(false, store.Id);
                    if (!languages.Any())
                    {
                        // language-less stores aren't allowed but could exist accidentally. Correct this.
                        var firstStoreLang = (async ? await GetAllLanguagesAsync(true, store.Id) : GetAllLanguages(true, store.Id)).FirstOrDefault();
                        if (firstStoreLang == null)
                        {
                            // absolute fallback
                            firstStoreLang = (async ? await GetAllLanguagesAsync(true) : GetAllLanguages(true)).FirstOrDefault();
                        }
                        map.Add(store.Id, new LanguageStub { Id = firstStoreLang.Id, UniqueSeoCode = firstStoreLang.UniqueSeoCode });
                    }
                    else
                    {
                        foreach (var lang in languages)
                        {
                            map.Add(store.Id, new LanguageStub { Id = lang.Id, UniqueSeoCode = lang.UniqueSeoCode });
                        }
                    }
                }

                return map;
            });

            return result;
        }

        #endregion
    }
}