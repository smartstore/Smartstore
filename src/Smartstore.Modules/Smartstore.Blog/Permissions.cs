using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Blog
{
    internal static class BlogPermissions
    {
        public const string Self = "cms.blog";
        public const string Read = "cms.blog.read";
        public const string Update = "cms.blog.update";
        public const string Create = "cms.blog.create";
        public const string Delete = "cms.blog.delete";
        public const string EditComment = "cms.blog.editcomment";
    }

    internal class BlogPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            // Get all permissions from above static class.
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(BlogPermissions));
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
                        new PermissionRecord { SystemName = BlogPermissions.Self }
                    }
                }
            };
        }
    }
}
