using Smartstore.Core.Security;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<AclRecord> AclRecords { get; set; }
        public DbSet<PermissionRecord> PermissionRecords { get; set; }
        public DbSet<PermissionRoleMapping> PermissionRoleMappings { get; set; }
    }
}
