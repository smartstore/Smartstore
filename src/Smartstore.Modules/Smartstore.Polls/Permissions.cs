using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace Smartstore.Polls
{
    internal static class PollPermissions
    {
        public const string Self = "cms.polls";
        public const string Read = "cms.polls.read";
        public const string Update = "cms.polls.update";
        public const string Create = "cms.polls.create";
        public const string Delete = "cms.polls.delete";
        public const string EditAnswer = "cms.poll.editanswer";
    }

    internal class PollPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            // Get all permissions from above static class.
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(PollPermissions));
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
                        new PermissionRecord { SystemName = PollPermissions.Self }
                    }
                }
            };
        }
    }
}
