#nullable enable

using Microsoft.AspNetCore.Http;
using Smartstore.Core.Identity;
using Smartstore.Core.Web;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Provides methods to protect the system from overload attacks, 
    /// such as too many guest users, excessive bot activity, etc.
    /// </summary>
    public interface IOverloadProtector
    {
        /// <summary>
        /// Determines if a guest user should be denied access due to exceeding 
        /// the allowed rate limit for guest users. 
        /// </summary>
        /// <remarks>
        /// The rate limit is applied based on a predefined policy, which 
        /// include thresholds for requests per time window and peak detection strategies. 
        /// If the guest user exceeds the limits, this method will return <c>true</c>, 
        /// indicating that the request should be denied.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, 
        /// containing a <see cref="bool"/> that indicates whether the guest user should be denied.
        /// </returns>
        Task<bool> DenyGuestAsync(HttpContext httpContext, Customer? customer = null);

        /// <summary>
        /// Determines if a bot should be denied access due to exceeding the allowed 
        /// rate limit for bot activity.
        /// </summary>
        /// <remarks>
        /// Bots typically have stricter rate-limiting policies compared to guests, 
        /// as they may generate high volumes of traffic. This method applies a bot-specific 
        /// rate limit to protect the system from excessive bot activity. If the bot exceeds 
        /// these limits, this method will return <c>true</c>.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, 
        /// containing a <see cref="bool"/> that indicates whether the bot should be denied.
        /// </returns>
        Task<bool> DenyBotAsync(HttpContext httpContext, IUserAgent userAgent);

        /// <summary>
        /// Determines if access for new guest users should be forbidden based on the current resiliency policy.
        /// </summary>
        /// <remarks>
        /// This can be used to temporarily or permanently block new guests 
        /// during periods of high load or maintenance.
        /// </remarks>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, 
        /// containing a <see cref="bool"/> that indicates whether new guest users should be forbidden.
        /// </returns>
        Task<bool> ForbidNewGuestAsync(HttpContext? httpContext);
    }
}
