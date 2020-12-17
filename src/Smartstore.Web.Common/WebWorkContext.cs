using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Web
{
    public partial class WebWorkContext : IWorkContext
    {
        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILanguageResolver _languageResolver;
        private readonly IStoreContext _storeContext;
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
            ILanguageResolver languageResolver,
            IStoreContext storeContext,
            IGenericAttributeService attrService,
            TaxSettings taxSettings,
            ICacheManager cache)
        {
            // TODO: (core) Implement WebWorkContext
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _languageResolver = languageResolver;
            _storeContext = storeContext;
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
                if (_language == null)
                {
                    var customer = CurrentCustomer;

                    // Resolve the current working language
                    _language = _languageResolver.ResolveLanguageAsync(customer, _httpContextAccessor.HttpContext).Await();

                    // Set language if current customer langid does not match resolved language id
                    var customerAttributes = customer.GenericAttributes;
                    if (customerAttributes.LanguageId != _language.Id)
                    {
                        SetCustomerLanguage(_language.Id);
                    }
                }

                return _language;
            }
            set
            {
                SetCustomerLanguage(value?.Id);
                _language = null;
            }
        }

        private void SetCustomerLanguage(int? languageId)
        {
            var customer = CurrentCustomer;

            if (customer == null || customer.IsSystemAccount)
                return;

            customer.GenericAttributes.LanguageId = languageId;
            customer.GenericAttributes.SaveChanges();
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
