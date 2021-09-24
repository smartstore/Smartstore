using System.Runtime.CompilerServices;
using Smartstore.Core.Identity;

namespace Smartstore.Forums
{
    public static partial class CustomerExtensions
    {
        /// <summary>
        /// Gets a value indicating whether customer is a forum moderator.
        /// </summary>
        /// <param name="onlyActiveRoles">A value indicating whether we should look only in active customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsForumModerator(this Customer customer, bool onlyActiveRoles = true)
        {
            // TODO: (mg) (core) Customer.IsInRole only works if CustomerRoleMappings and CustomerRoleMappings.CustomerRole are included (very error-prone)!!
            return customer.IsInRole(SystemCustomerRoleNames.ForumModerators, onlyActiveRoles);
        }

        /// <summary>
        /// Gets a value indicating whether the customer is allowed to subscribe to a forum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAllowedToSubscribe(this Customer customer)
        {
            return !customer.IsGuest();
        }
    }
}
