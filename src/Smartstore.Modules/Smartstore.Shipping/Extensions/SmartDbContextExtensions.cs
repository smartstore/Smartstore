using Smartstore.Core.Data;

namespace Smartstore.Shipping
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<ShippingRateByTotal> ShippingRatesByTotal(this SmartDbContext db)
            => db.Set<ShippingRateByTotal>();
    }
}