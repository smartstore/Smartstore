using Smartstore.Core.Data;

namespace Smartstore.ShippingByWeight
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<ShippingRateByWeight> ShippingRatesByWeight(this SmartDbContext db)
            => db.Set<ShippingRateByWeight>();
    }
}