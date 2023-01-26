using Smartstore.Core.Identity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents a provider to add permissions.
    /// </summary>
    public interface IPermissionProvider
    {
        /// <summary>
        /// Gets a list of <see cref="PermissionRecord"/> to be added.
        /// The permissions are automatically installed when the application is restarted or a module is installed.
        /// </summary>
        IEnumerable<PermissionRecord> GetPermissions();

        /// <summary>
        /// Gets a list of default permissions which are automatically granted to the given customer role.
        /// Typically used for <see cref="SystemCustomerRoleNames.Administrators"/> to grant him the root permission.
        /// </summary>
        /// <returns></returns>
        IEnumerable<DefaultPermissionRecord> GetDefaultPermissions();
    }
}