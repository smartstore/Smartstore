using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Tax;
using Smartstore.Core.Tax.Settings;

namespace Smartstore.Web.Common
{
    public partial class WebWorkContext : IWorkContext
    {
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoreContext _storeContext;
        private readonly ILanguageService _languageService;
        private readonly IGenericAttributeService _attrService;
        private readonly TaxSettings _taxSettings;
        private readonly ICacheManager _cache;

        private TaxDisplayType? _taxDisplayType;
        private Language _language;
        private Customer _customer;
        private Currency _currency;
        private Customer _impersonator;
        private bool? _isAdminArea;

        public WebWorkContext(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IStoreContext storeContext,
            ILanguageService languageService,
            IGenericAttributeService attrService,
            TaxSettings taxSettings,
            ICacheManager cache)
        {
            // TODO: (core) Implement WebWorkContext
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _storeContext = storeContext;
            _languageService = languageService;
            _attrService = attrService;
            _taxSettings = taxSettings;
            _cache = cache;
        }

        public Customer CurrentCustomer 
        {
            get 
            {
                if (_customer != null)
                {
                    return _customer;
                }

                _customer = _db.Customers.FirstOrDefault();

                return _customer;
            }
            set => _customer = value; 
        }

        public Customer CurrentImpersonator => _impersonator;

        public Language WorkingLanguage 
        {
            get
            {
                if (_language != null)
                {
                    return _language;
                }

                SmartProviderCultureResult cultureResult = null;

                var requestCultureFeature = _httpContextAccessor.HttpContext?.Features?.Get<IRequestCultureFeature>();

                if (requestCultureFeature != null)
                {
                    var requestCulture = requestCultureFeature.RequestCulture.Culture;
                    cultureResult = (requestCultureFeature.Provider as SmartRequestCultureProvider)?.GetResult(_httpContextAccessor.HttpContext);

                    _language = cultureResult?.Language 
                        ?? _db.Languages.FirstOrDefault(x => x.UniqueSeoCode == requestCulture.TwoLetterISOLanguageName);

                    if (cultureResult != null && !cultureResult.IsFallback && cultureResult.CustomerLanguageId != _language.Id)
                    {
                        SetCustomerLanguage(_language.Id, _storeContext.CurrentStore.Id);
                    }
                }
                else
                {
                    _language = _db.Languages.FindById(_languageService.GetDefaultLanguageId());
                }

                return _language;
            }
            set
            {
                SetCustomerLanguage(value?.Id, _storeContext.CurrentStore.Id);
                _language = null;
            }
        }

        private void SetCustomerLanguage(int? languageId, int storeId)
        {
            if (CurrentCustomer != null || CurrentCustomer.IsSystemAccount)
                return;

            _attrService.ApplyAttribute(CurrentCustomer.Id, SystemCustomerAttributeNames.LanguageId, CurrentCustomer.GetEntityName(), languageId, storeId);
            _db.SaveChanges();
        }

        public Currency WorkingCurrency
        {
            get
            {
                if (_currency != null)
                {
                    return _currency;
                }

                _currency = _db.Currencies.AsNoTracking().FirstOrDefault();

                return _currency;
            }
            set => _currency = value;
        }

        public TaxDisplayType TaxDisplayType 
        {
            get => GetTaxDisplayTypeFor(CurrentCustomer, _storeContext.CurrentStore.Id);
            set => _taxDisplayType = value;
        }

        public TaxDisplayType GetTaxDisplayTypeFor(Customer customer, int storeId)
        {
            return TaxDisplayType.IncludingTax;
        }

        public bool IsAdminArea 
        { 
            get => false; 
            set => _isAdminArea = value; 
        }
    }
}
