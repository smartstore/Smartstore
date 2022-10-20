using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Localization
{
    public class LanguageResolver : ILanguageResolver
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ILanguageService _languageService;
        private readonly LocalizationSettings _localizationSettings;

        private readonly AcceptLanguageHeaderRequestCultureProvider _acceptHeaderProvider = new();

        public LanguageResolver(
            SmartDbContext db,
            IStoreContext storeContext,
            ILanguageService languageService,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _storeContext = storeContext;
            _languageService = languageService;
            _localizationSettings = localizationSettings;
        }

        public Language ResolveLanguage(Customer currentCustomer, HttpContext httpContext)
            => ResolveLanguageCore(currentCustomer, httpContext, false).Await();

        public Task<Language> ResolveLanguageAsync(Customer currentCustomer, HttpContext httpContext)
            => ResolveLanguageCore(currentCustomer, httpContext, true);

        protected virtual async Task<Language> ResolveLanguageCore(Customer currentCustomer, HttpContext httpContext, bool async)
        {
            Guard.NotNull(currentCustomer, nameof(currentCustomer));

            int storeId = _storeContext.CurrentStore.Id;

            int customerLangId = currentCustomer.IsSystemAccount
                ? (httpContext != null ? httpContext.Request.Query["lid"].FirstOrDefault().ToInt() : 0)
                : currentCustomer.GenericAttributes.LanguageId ?? 0;

            if (httpContext == null)
            {
                return await GetDefaultLanguage(customerLangId, storeId, async);
            }

            return
                // 1: Try resolve from route values or from request path
                await ResolveFromRoute(httpContext, storeId, async) ??
                // 2: Try resolve from determined customer lang id
                await ResolveFromCustomer(customerLangId, storeId, async) ??
                // 3: Try resolve from accept header
                await ResolveFromAcceptHeader(httpContext, storeId, customerLangId, currentCustomer, async) ??
                // 3: Get default fallback language
                await GetDefaultLanguage(customerLangId, storeId, async) ??
                // Should never happen
                throw new InvalidOperationException("At least one language must be active!");
        }

        protected virtual async Task<Language> ResolveFromRoute(HttpContext httpContext, int storeId, bool async)
        {
            if (!_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                return null;
            }

            var cultureCode = httpContext.GetCultureCode();
            if (cultureCode.IsEmpty() || !_languageService.IsPublishedLanguage(cultureCode, storeId))
            {
                return null;
            }

            return async
                ? await _db.Languages.FirstOrDefaultAsync(x => x.UniqueSeoCode == cultureCode)
                : _db.Languages.FirstOrDefault(x => x.UniqueSeoCode == cultureCode);
        }

        protected virtual async Task<Language> ResolveFromCustomer(int customerLangId, int storeId, bool async)
        {
            if (customerLangId > 0 && _languageService.IsPublishedLanguage(customerLangId, storeId))
            {
                return async
                    ? await _db.Languages.FindByIdAsync(customerLangId)
                    : _db.Languages.FindById(customerLangId);
            }

            return null;
        }

        protected virtual async Task<Language> ResolveFromAcceptHeader(HttpContext httpContext, int storeId, int customerLangId, Customer customer, bool async)
        {
            if (!_localizationSettings.DetectBrowserUserLanguage || customer.IsSystemAccount)
            {
                return null;
            }

            var providerResult = await _acceptHeaderProvider.DetermineProviderCultureResult(httpContext);
            if (providerResult != null)
            {
                foreach (var culture in providerResult.Cultures)
                {
                    var language = async ?
                        await _db.Languages.FirstOrDefaultAsync(x => x.LanguageCulture == culture.Value || x.UniqueSeoCode == culture.Value)
                        : _db.Languages.FirstOrDefault(x => x.LanguageCulture == culture.Value || x.UniqueSeoCode == culture.Value);

                    if (language != null && _languageService.IsPublishedLanguage(language.Id, storeId))
                    {
                        return language;
                    }
                }
            }

            return null;
        }

        protected virtual async Task<Language> GetDefaultLanguage(int customerLangId, int storeId, bool async)
        {
            if (customerLangId == 0 || !_languageService.IsPublishedLanguage(customerLangId, storeId))
            {
                customerLangId = _languageService.GetMasterLanguageId(storeId);
            }

            return async ? await _db.Languages.FindByIdAsync(customerLangId) : _db.Languages.FindById(customerLangId);
        }
    }
}