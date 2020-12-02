using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Localization
{
    public class SmartProviderCultureResult : ProviderCultureResult
    {
        public SmartProviderCultureResult(StringSegment culture)
            : base(culture)
        {
        }

        public RequestCultureProvider SourceProvider { get; set; }
        public Language Language { get; set; }

        public int StoreId { get; init; }
        public int CustomerLanguageId { get; init; }
        public bool IsFallback { get; init; }
    }
    
    public class SmartRequestCultureProvider : RequestCultureProvider
    {
        private readonly RequestCultureProvider _routeProvider = new RouteDataRequestCultureProvider();
        private readonly RequestCultureProvider _acceptHeaderProvider = new AcceptLanguageHeaderRequestCultureProvider();

        public override async Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext));

            var services = httpContext.RequestServices;
            var workContext = services.GetRequiredService<IWorkContext>();
            var storeContext = services.GetRequiredService<IStoreContext>();
            var languageService = services.GetRequiredService<ILanguageService>();
            var attrService = services.GetRequiredService<IGenericAttributeService>();
            var localizationSettings = services.GetRequiredService<LocalizationSettings>();

            var storeId = storeContext.CurrentStore.Id;
            var customer = workContext.CurrentCustomer;
            int customerLangId = 0;

            SmartProviderCultureResult validResult;

            if (customer != null)
            {
                customerLangId = customer.IsSystemAccount
                    ? httpContext.Request.Query["lid"].FirstOrDefault().ToInt()
                    : await customer.GetAttributeAsync<int>(SystemCustomerAttributeNames.LanguageId, attrService, storeId);
            }

            if (localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                // Try get language from URL/Route
                var routeResult = await _routeProvider.DetermineProviderCultureResult(httpContext);
                if (ValidateResult(routeResult, out validResult))
                {
                    validResult.SourceProvider = _routeProvider;
                    return PublishResult(validResult);
                }
            }

            if (localizationSettings.DetectBrowserUserLanguage && !customer.IsSystemAccount && (customerLangId == 0 || !languageService.IsPublishedLanguage(customerLangId, storeId)))
            {
                // Try get language from accept header
                if (ValidateResult(await _acceptHeaderProvider.DetermineProviderCultureResult(httpContext), out validResult))
                {
                    validResult.SourceProvider = _acceptHeaderProvider;
                    return PublishResult(validResult);
                }
            }

            var db = services.GetRequiredService<SmartDbContext>();

            if (customerLangId > 0 && languageService.IsPublishedLanguage(customerLangId, storeId))
            {
                // Get customer user language
                var language = await db.Languages.FindByIdAsync(customerLangId);
                return PublishResult(new SmartProviderCultureResult(language.GetTwoLetterISOLanguageName())
                {
                    Language = language,
                    CustomerLanguageId = customerLangId,
                    StoreId = storeId
                });
            }

            // Fallback
            return PublishResult(new SmartProviderCultureResult(languageService.GetDefaultLanguageSeoCode(storeId))
            {
                CustomerLanguageId = customerLangId,
                StoreId = storeId,
                IsFallback = true
            });

            bool ValidateResult(ProviderCultureResult result, out SmartProviderCultureResult validResult)
            {
                validResult = null;

                if (result == null || result.Cultures.Count == 0)
                {
                    return false;
                }

                foreach (var culture in result.Cultures)
                {
                    if (languageService.IsPublishedLanguage(culture.Value, storeId))
                    {
                        validResult = new SmartProviderCultureResult(culture) 
                        { 
                            CustomerLanguageId = customerLangId, 
                            StoreId = storeId
                        };

                        return true;
                    }
                }

                return false;
            }

            SmartProviderCultureResult PublishResult(SmartProviderCultureResult validResult)
            {
                httpContext.Items["SmartProviderCultureResult"] = validResult;
                return validResult;
            }
        }

        public SmartProviderCultureResult GetResult(HttpContext httpContext)
        {
            return httpContext?.GetItem<SmartProviderCultureResult>("SmartProviderCultureResult", forceCreation: false);
        }
    }
}
