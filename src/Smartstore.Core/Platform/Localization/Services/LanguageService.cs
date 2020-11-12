using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Threading;

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
        private readonly IStoreMappingService _storeMappingService;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly ISettingFactory _settingFactory;
        private readonly Lazy<LocalizationSettings> _localizationSettings;

        public LanguageService(
            ICacheManager cache,
            SmartDbContext db,
            ISettingFactory settingFactory,
            Lazy<LocalizationSettings> localizationSettings,
            IStoreMappingService storeMappingService,
            IStoreContext storeContext)
        {
            _cache = cache;
            _db = db;
            _settingFactory = settingFactory;
            _localizationSettings = localizationSettings;
            _storeMappingService = storeMappingService;
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

        #endregion

        #region ILanguageService

        public virtual List<Language> GetAllLanguages(bool includeHidden = false, int storeId = 0)
        {
            return _db.Languages.ApplyStandardFilter(includeHidden, storeId).ToList();
        }

        public virtual async Task<List<Language>> GetAllLanguagesAsync(bool includeHidden = false, int storeId = 0)
        {
            return await _db.Languages.ApplyStandardFilter(includeHidden, storeId).ToListAsync();
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

            var map = await GetStoreLanguageMapAsync().ConfigureAwait(false);
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

            var map = await GetStoreLanguageMapAsync().ConfigureAwait(false);
            if (map.ContainsKey(storeId))
            {
                return map[storeId].Any(x => x.UniqueSeoCode == seoCode);
            }

            return false;
        }

        public virtual string GetDefaultLanguageSeoCode(int storeId = 0)
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

        public virtual async Task<string> GetDefaultLanguageSeoCodeAsync(int storeId = 0)
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

        public virtual int GetDefaultLanguageId(int storeId = 0)
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

        public virtual async Task<int> GetDefaultLanguageIdAsync(int storeId = 0)
        {
            if (storeId <= 0)
                storeId = _storeContext.CurrentStore.Id;

            var map = await GetStoreLanguageMapAsync().ConfigureAwait(false);
            if (map.ContainsKey(storeId))
            {
                return map[storeId].FirstOrDefault().Id;
            }

            return 0;
        }

        protected Multimap<int, LanguageStub> GetStoreLanguageMap()
        {
            // TODO: (core) We should avoid this!?
            return AsyncRunner.RunSync(GetStoreLanguageMapAsync);
        }

        /// <summary>
        /// Gets a map of active/published store languages
        /// </summary>
        /// <returns>A map of store languages where key is the store id and values are tuples of language ids and seo codes</returns>
        protected virtual async Task<Multimap<int, LanguageStub>> GetStoreLanguageMapAsync()
        {
            var result = await _cache.GetAsync(STORE_LANGUAGE_MAP_KEY, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));
                
                var map = new Multimap<int, LanguageStub>();

                var allStores = _storeContext.GetAllStores();
                foreach (var store in allStores)
                {
                    var languages = await GetAllLanguagesAsync(false, store.Id);
                    if (!languages.Any())
                    {
                        // language-less stores aren't allowed but could exist accidentally. Correct this.
                        var firstStoreLang = (await GetAllLanguagesAsync(true, store.Id)).FirstOrDefault();
                        if (firstStoreLang == null)
                        {
                            // absolute fallback
                            firstStoreLang = (await GetAllLanguagesAsync(true)).FirstOrDefault();
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