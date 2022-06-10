using Smartstore.Core.Identity;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerRole> CustomerRoles { get; set; }
        public DbSet<CustomerRoleMapping> CustomerRoleMappings { get; set; }
        public DbSet<CustomerContent> CustomerContent { get; set; }
        public DbSet<RewardPointsHistory> RewardPointsHistory { get; set; }
        public DbSet<WalletHistory> WalletHistory { get; set; }
        public DbSet<ExternalAuthenticationRecord> ExternalAuthenticationRecords { get; set; }
    }
}