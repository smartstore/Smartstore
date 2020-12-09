using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Orders;
using Smartstore.Core.Tax;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
    }
}