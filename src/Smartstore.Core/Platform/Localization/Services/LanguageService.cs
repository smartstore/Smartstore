using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
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

    public partial class LanguageService : AsyncDbSaveHook<Language>
    {
        private const string LANGUAGES_COUNT = "SmartStore.language.count-{0}";
        private const string LANGUAGES_PATTERN_KEY = "SmartStore.language.*";

        private readonly SmartDbContext _db;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IStoreContext _storeContext;
        private readonly IRequestCache _requestCache;
        private readonly ICacheManager _cache;
        private readonly ISettingFactory _settingFactory;
        private readonly Lazy<LocalizationSettings> _localizationSettings;

        public LanguageService(
            IRequestCache requestCache,
            ICacheManager cache,
            SmartDbContext db,
            ISettingFactory settingFactory,
            Lazy<LocalizationSettings> localizationSettings,
            IStoreMappingService storeMappingService,
            IStoreContext storeContext)
        {
            // TODO: (core) EF 2nd level request caching: implement policy based with central configuration
            
            _requestCache = requestCache;
            _cache = cache;
            _db = db;
            _settingFactory = settingFactory;
            _localizationSettings = localizationSettings;
            _storeMappingService = storeMappingService;
            _storeContext = storeContext;
        }

        #region Hook

        protected override async Task<HookResult> OnDeletingAsync(Language entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            // Update default admin area language (if required)
            var localizationSettings = _localizationSettings.Value;
            if (localizationSettings.DefaultAdminLanguageId == entity.Id)
            {
                var allLanguages = await GetAllLanguagesAsync();
                foreach (var activeLanguage in allLanguages)
                {
                    if (activeLanguage.Id != entity.Id)
                    {
                        localizationSettings.DefaultAdminLanguageId = activeLanguage.Id;
                        await _settingFactory.SaveSettingsAsync(localizationSettings);
                        break;
                    }
                }
            }

            return HookResult.Ok;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            _requestCache.RemoveByPattern(LANGUAGES_PATTERN_KEY);
            return Task.FromResult(HookResult.Ok);
        }

        #endregion

        #region ILanguageService

        public virtual async Task<List<Language>> GetAllLanguagesAsync(bool includeHidden = false, int storeId = 0)
        {
            var cacheKey = "db.lang.all.{0}".FormatInvariant(includeHidden);

            var languages = await _requestCache.Get(cacheKey, async () => 
            {
                var query = _db.Languages.AsQueryable();

                if (!includeHidden)
                {
                    query = query.Where(x => x.Published);
                }

                query = query.OrderBy(x => x.DisplayOrder);

                return await query.ToListAsync();
            });

            // store mapping
            if (storeId > 0)
            {
                languages = (await languages
                    .WhereAsync(x => _storeMappingService.AuthorizeAsync(x, storeId)))
                    .ToList();
            }

            return languages;
        }

        #endregion
    }
}