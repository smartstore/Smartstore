using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerRole> CustomerRoles { get; set; }
    }
}