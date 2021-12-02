using Microsoft.EntityFrameworkCore;
using Smartstore.ShippingByWeight.Domain;
using Smartstore.Core.Data;

namespace Smartstore.ShippingByWeight
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<ShippingRateByWeight> ShippingRatesByWeight(this SmartDbContext db)
            => db.Set<ShippingRateByWeight>();
    }
}