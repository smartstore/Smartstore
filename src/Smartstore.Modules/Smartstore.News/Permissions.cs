using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.News
{
    internal static class NewsPermissions
    {
        public const string Self = "cms.news";
        public const string Read = "cms.news.read";
        public const string Update = "cms.news.update";
        public const string Create = "cms.news.create";
        public const string Delete = "cms.news.delete";
        public const string EditComment = "cms.news.editcomment";
    }

    internal class NewsPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            // Get all permissions from above static class.
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(NewsPermissions));
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
                        new PermissionRecord { SystemName = NewsPermissions.Self }
                    }
                }
            };
        }
    }
}
