using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core
{
    public class InstallationWorkContextSource : IWorkContextSource
    {
        private readonly SmartDbContext _db;

        public InstallationWorkContextSource(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<(Customer, Customer)> ResolveCurrentCustomerAsync()
        {
            return (await _db.Customers.FirstAsync(), null);
        }

        public Task<Language> ResolveWorkingLanguageAsync(Customer customer)
        {
            return _db.Languages.FirstAsync();
        }

        public Task<Currency> ResolveWorkingCurrencyAsync(Customer customer, bool forAdminArea)
        {
            return _db.Currencies.FirstAsync();
        }

        public Task<TaxDisplayType> ResolveTaxDisplayTypeAsync(Customer customer, int storeId)
        {
            return Task.FromResult(TaxDisplayType.IncludingTax);
        }

        public Task SaveCustomerAttribute(Customer customer, string name, int? value, bool async)
        {
            return Task.CompletedTask;
        }
    }
}
