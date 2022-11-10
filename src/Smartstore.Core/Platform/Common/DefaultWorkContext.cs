using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Core
{
    public partial class DefaultWorkContext : IWorkContext
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContextSource _source;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoreContext _storeContext;
        private readonly TaxSettings _taxSettings;

        // KeyItem1 = CustomerId, KeyItem2 = StoreId
        private readonly Dictionary<(int, int), TaxDisplayType> _taxDisplayTypes = new();

        private Customer _customer;
        private Language _language;
        private Currency _currency;
        private Customer _impersonator;
        private TaxDisplayType? _taxDisplayType;
        private bool? _isAdminArea;

        public DefaultWorkContext(
            SmartDbContext db,
            IWorkContextSource source,
            IHttpContextAccessor httpContextAccessor,
            IStoreContext storeContext,
            TaxSettings taxSettings)
        {
            _db = db;
            _source = source;
            _httpContextAccessor = httpContextAccessor;
            _storeContext = storeContext;
            _taxSettings = taxSettings;
        }

        public async Task InitializeAsync()
        {
            if (_customer == null)
            {
                (_customer, _impersonator) = await _source.ResolveCurrentCustomerAsync();
            }

            if (_language == null)
            {
                _language = await _source.ResolveWorkingLanguageAsync(_customer);
            }

            if (_currency == null)
            {
                _currency = await _source.ResolveWorkingCurrencyAsync(_customer, IsAdminArea);
            }

            if (_taxDisplayType == null)
            {
                _taxDisplayType = await GetTaxDisplayTypeAsync(_customer, _storeContext.CurrentStore.Id);
            }
        }

        public bool IsInitialized
        {
            get => _customer != null && _language != null && _currency != null && _taxDisplayType.HasValue;
        }

        public Customer CurrentCustomer
        {
            get
            {
                if (_customer == null)
                {
                    InitializeAsync().Await();
                }

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
                    InitializeAsync().Await();
                }

                return _language;
            }
            set
            {
                if (value?.Id != _language?.Id)
                {
                    _source.SaveCustomerAttribute(CurrentCustomer, SystemCustomerAttributeNames.LanguageId, value?.Id, false).Await();
                    _language = value;
                }
            }
        }

        public Currency WorkingCurrency
        {
            get
            {
                if (_currency == null)
                {
                    InitializeAsync().Await();
                }

                return _currency;
            }
            set
            {
                if (value?.Id != _currency?.Id)
                {
                    _source.SaveCustomerAttribute(CurrentCustomer, SystemCustomerAttributeNames.CurrencyId, value?.Id, false).Await();
                    _currency = value;
                }
            }
        }

        public TaxDisplayType TaxDisplayType
        {
            get
            {
                if (_taxDisplayType == null)
                {
                    InitializeAsync().Await();
                }

                return _taxDisplayType.Value;
            }
            set
            {
                if (_taxSettings.AllowCustomersToSelectTaxDisplayType && CurrentCustomer.TaxDisplayTypeId != (int)value)
                {
                    CurrentCustomer.TaxDisplayTypeId = (int)value;
                    _db.SaveChanges();
                }

                _taxDisplayTypes[(CurrentCustomer.Id, _storeContext.CurrentStore.Id)] = value;
            }
        }

        public async Task<TaxDisplayType> GetTaxDisplayTypeAsync(Customer customer, int storeId)
        {
            Guard.NotNull(customer, nameof(customer));

            var key = (_customer.Id, storeId);

            if (!_taxDisplayTypes.TryGetValue(key, out var result))
            {
                result = await _source.ResolveTaxDisplayTypeAsync(_customer, storeId);
                _taxDisplayTypes[key] = result;
            }

            return result;
        }

        public bool IsAdminArea
        {
            get
            {
                if (_isAdminArea.HasValue)
                {
                    return _isAdminArea.Value;
                }

                var httpContext = _httpContextAccessor.HttpContext;
                _isAdminArea = httpContext?.Request?.IsAdminArea() == true;

                return _isAdminArea.Value;
            }
            set => _isAdminArea = value;
        }
    }
}
