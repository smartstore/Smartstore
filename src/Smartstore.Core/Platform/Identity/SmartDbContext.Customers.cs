using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerRole> CustomerRoles { get; set; }
        public DbSet<CustomerRoleMapping> CustomerRoleMappings { get; set; }
    }
}