using Smartstore.Core.Data;

namespace Smartstore.Tax
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<TaxRateEntity> TaxRates(this SmartDbContext db)
            => db.Set<TaxRateEntity>();
    }
}
