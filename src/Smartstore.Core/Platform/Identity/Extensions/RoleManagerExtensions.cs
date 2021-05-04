using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Smartstore.Core.Identity
{
    public static partial class RoleManagerExtensions
    {
        /// <summary>
        /// Overload to pass roleId directly as int.
        /// </summary>
        public static Task<CustomerRole> FindByIdAsync(this RoleManager<CustomerRole> roleManager, int roleId)
        {
            Guard.NotZero(roleId, nameof(roleId));

            return roleManager.FindByIdAsync(roleId.ToString());
        }
    }
}
