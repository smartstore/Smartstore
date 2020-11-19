using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Common;
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
            TaxSettings taxSettings,
            ICacheManager cache)
        {
            // TODO: (core) Implement WebWorkContext
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _storeContext = storeContext;
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

                _language = _db.Languages.AsNoTracking().FirstOrDefault();

                return _language;
            }
            set => _language = value;
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
