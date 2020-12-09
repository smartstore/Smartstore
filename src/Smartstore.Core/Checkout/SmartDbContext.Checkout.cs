using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
    }
}