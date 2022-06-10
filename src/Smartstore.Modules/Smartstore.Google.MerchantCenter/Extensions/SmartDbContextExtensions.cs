using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Google.MerchantCenter.Domain;

namespace Smartstore.Google.MerchantCenter
{
    public static class SmartDbContextExtensions
    {
        public static DbSet<GoogleProduct> GoogleProducts(this SmartDbContext db)
            => db.Set<GoogleProduct>();
    }
}