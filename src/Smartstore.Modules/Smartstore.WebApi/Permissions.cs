using Smartstore.Core.Identity;

namespace Smartstore.Web.Api
{
    internal static class WebApiPermissions
    {
        public const string Self = "webapi";
        public const string Read = "webapi.read";
        public const string Update = "webapi.update";
        public const string Create = "webapi.create";
        public const string Delete = "webapi.delete";
    }

    internal class WebApiPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(WebApiPermissions));
            var permissions = permissionSystemNames.Select(x => new PermissionRecord { SystemName = x });

            return permissions;
        }

        public IEnumerable<DefaultPermissionRecord> GetDefaultPermissions()
        {
            return new[]
            {
                new DefaultPermissionRecord
                {
                    CustomerRoleSystemName = SystemCustomerRoleNames.Administrators,
                    PermissionRecords = new[]
                    {
                        new PermissionRecord { SystemName = WebApiPermissions.Self }
                    }
                }
            };
        }
    }
}
