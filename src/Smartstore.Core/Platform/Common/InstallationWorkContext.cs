using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core
{
    public class InstallationWorkContext : IWorkContext
    {
        private readonly SmartDbContext _db;

        private Customer _customer;
        private Language _language;
        private Currency _currency;

        public InstallationWorkContext(SmartDbContext db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            if (_customer == null)
            {
                _customer = await _db.Customers.FirstAsync();
            }

            if (_language == null)
            {
                _language = await _db.Languages.FirstAsync();
            }

            if (_currency == null)
            {
                _currency = await _db.Currencies.FirstAsync();
            }
        }

        public bool IsInitialized
        {
            get => _customer != null && _language != null && _currency != null;
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

        public Customer CurrentImpersonator => null;

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
                _language = value;
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
                _currency = value;
            }
        }

        public TaxDisplayType TaxDisplayType { get; set; }

        public TaxDisplayType GetTaxDisplayTypeFor(Customer customer, int storeId)
            => TaxDisplayType.IncludingTax;

        public bool IsAdminArea { get; set; }
    }
}
