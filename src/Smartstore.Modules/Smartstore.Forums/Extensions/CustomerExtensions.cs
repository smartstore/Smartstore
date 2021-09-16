using System.Runtime.CompilerServices;
using Smartstore.Core.Identity;

namespace Smartstore.Forums
{
    internal static partial class CustomerExtensions
    {
        /// <summary>
        /// Gets a value indicating whether customer is a forum moderator.
        /// </summary>
        /// <param name="onlyActiveRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsForumModerator(this Customer customer, bool onlyActiveRoles = true)
        {
            return customer.IsInRole(SystemCustomerRoleNames.ForumModerators, onlyActiveRoles);
        }
    }
}
