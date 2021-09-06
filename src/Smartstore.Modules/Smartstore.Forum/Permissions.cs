using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Forum
{
    internal static class ForumPermissions
    {
        public static partial class Cms
        {
            public static class Forum
            {
                public const string Self = "cms.forum";
                public const string Read = "cms.forum.read";
                public const string Update = "cms.forum.update";
                public const string Create = "cms.forum.create";
                public const string Delete = "cms.forum.delete";
            }
        }
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
                        new PermissionRecord { SystemName = ForumPermissions.Cms.Forum.Self }
                    }
                }
            };
        }
    }
}
