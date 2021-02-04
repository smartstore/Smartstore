using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
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

        public virtual async Task<Language> ResolveLanguageAsync(Customer currentCustomer, HttpContext httpContext)
        {
            Guard.NotNull(currentCustomer, nameof(currentCustomer));

            int storeId = _storeContext.CurrentStore.Id;
            
            int customerLangId = currentCustomer.IsSystemAccount
                ? (httpContext != null ? httpContext.Request.Query["lid"].FirstOrDefault().ToInt() : 0)
                : currentCustomer.GenericAttributes.LanguageId ?? 0;

            if (httpContext == null)
            {
                return await GetDefaultLanguage(customerLangId, storeId);
            }

            return
                // 1: Try resolve from route values or from request path
                await ResolveFromRouteAsync(httpContext, storeId) ??
                // 2: Try resolve from determined customer lang id
                await ResolveFromCustomerAsync(customerLangId, storeId) ??
                // 3: Try resolve from accept header
                await ResolveFromAcceptHeaderAsync(httpContext, storeId, customerLangId, currentCustomer) ??
                // 3: Get default fallback language
                await GetDefaultLanguage(customerLangId, storeId) ??
                // Should never happen
                throw new SmartException("At least one language must be active!");
        }

        private async Task<Language> ResolveFromRouteAsync(HttpContext httpContext, int storeId)
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

            return await _db.Languages.FirstOrDefaultAsync(x => x.UniqueSeoCode == cultureCode);
        }

        private async Task<Language> ResolveFromCustomerAsync(int customerLangId, int storeId)
        {
            if (customerLangId > 0 && _languageService.IsPublishedLanguage(customerLangId, storeId))
            {
                return await _db.Languages.FindByIdAsync(customerLangId);
            }

            return null;
        }

        private async Task<Language> ResolveFromAcceptHeaderAsync(HttpContext httpContext, int storeId, int customerLangId, Customer customer)
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
                    var language =
                        await _db.Languages.FirstOrDefaultAsync(x => x.LanguageCulture == culture.Value || x.UniqueSeoCode == culture.Value);

                    if (language != null && _languageService.IsPublishedLanguage(language.Id, storeId))
                    {
                        return language;
                    }
                }
            }

            return null;
        }

        private async Task<Language> GetDefaultLanguage(int customerLangId, int storeId)
        {
            if (customerLangId == 0 || !_languageService.IsPublishedLanguage(customerLangId, storeId))
            {
                customerLangId = _languageService.GetMasterLanguageId(storeId);
            }

            return await _db.Languages.FindByIdAsync(customerLangId);
        }
    }
}