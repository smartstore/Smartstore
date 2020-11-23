using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Customers;
using Smartstore.Core.Tax;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
    }
}