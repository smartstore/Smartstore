using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Provides methods to protect the system from overload attacks, 
    /// like too many guest users, too many bots, etc.
    /// </summary>
    public interface IOverloadProtector
    {
        /// <summary>
        /// Returns <c>true</c> if the guest user should be denied due to the guest user rate limit policy.
        /// </summary>
        Task<bool> DenyGuestAsync();

        /// <summary>
        /// Returns <c>true</c> if the customer should be denied due to the authenticated customer rate limit policy.
        /// </summary>
        Task<bool> DenyCustomerAsync();

        /// <summary>
        /// Returns <c>true</c> if the bot should be denied due to the bot rate limit policy.
        /// </summary>
        Task<bool> DenyBotAsync();

        /// <summary>
        /// Returns <c>true</c> if access should be forbidden for new guest users.
        /// </summary>
        Task<bool> ForbidNewGuestAsync(HttpContext httpContext);
    }
}
