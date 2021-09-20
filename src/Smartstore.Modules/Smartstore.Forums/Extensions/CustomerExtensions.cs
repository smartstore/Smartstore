using System.Runtime.CompilerServices;
using Smartstore.Core.Identity;

namespace Smartstore.Forums
{
    // INFO: (mg) (core) We shouldn't limit 3rd party devs. Imagine a module dev wants to implement some forum extending module.
    public static partial class CustomerExtensions
    {
        /// <summary>
        /// Gets a value indicating whether customer is a forum moderator.
        /// </summary>
        /// <param name="onlyActiveRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsForumModerator(this Customer customer, bool onlyActiveRoles = true)
        {
            return customer.IsInRole(SystemCustomerRoleNames.ForumModerators, onlyActiveRoles);
        }
    }
}
