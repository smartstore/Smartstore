using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Forum
{
    internal static class ForumPermissions
    {
        public const string Self = "forum";
    }

    internal class ForumPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            // Get all permissions from above static class.
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(ForumPermissions));
            var permissions = permissionSystemNames.Select(x => new PermissionRecord { SystemName = x });

            return permissions;
        }

        public IEnumerable<DefaultPermissionRecord> GetDefaultPermissions()
        {
            // Allow root permission for admin by default.
            return new[]
            {
                new DefaultPermissionRecord
                {
                    CustomerRoleSystemName = SystemCustomerRoleNames.Administrators,
                    PermissionRecords = new[]
                    {
                        new PermissionRecord { SystemName = ForumPermissions.Self }
                    }
                }
            };
        }
    }
}
