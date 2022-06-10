using Microsoft.AspNetCore.Identity;

namespace Smartstore.Core.Identity
{
    public static partial class RoleManagerExtensions
    {
        /// <summary>
        /// Overload to pass roleId directly as int.
        /// </summary>
        public static Task<CustomerRole> FindByIdAsync(this RoleManager<CustomerRole> roleManager, int roleId)
            => roleManager.FindByIdAsync(roleId.ToString());
    }
}
