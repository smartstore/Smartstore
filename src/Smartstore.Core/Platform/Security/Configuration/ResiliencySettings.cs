using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// Represents the configuration settings for resiliency and overload protection in the system.
    /// These settings are used to control the behavior of overload protection mechanisms,
    /// such as limiting traffic for different user categories (guests, bots, etc.) and handling peak load scenarios.
    /// </summary>
    public class ResiliencySettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether overload protection is enabled.
        /// When set to <c>true</c>, the system applies the defined traffic limits and overload protection policies.
        /// If set to <c>false</c>, overload protection is disabled, and no traffic limits are enforced.
        /// </summary>
        public bool EnableOverloadProtection { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether NEW guest users should be forbidden
        /// if the request is a sub/secondary request, e.g., an AJAX request, POST,
        /// script, media file, etc.
        /// </summary>
        /// <remarks>
        /// This setting can be used to restrict the creation of new guest sessions 
        /// on successive (secondary) resource requests. A "bad bot" that does not accept cookies
        /// is difficult to identify as a bot and may create a new guest session with each (sub)-request,
        /// especially if it varies its client IP address and user agent string with each request.
        /// If set to <c>true</c>, new guests will be blocked under these circumstances.
        /// </remarks>
        public bool ForbidNewGuestsIfSubRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets the duration of the long traffic observation window.
        /// The long traffic window defines the period during which sustained traffic is measured.
        /// Use this setting to control traffic limits over a more extended period, 
        /// such as one minute or longer.
        /// </summary>
        public TimeSpan LongTrafficWindow { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the long traffic limit for guest users.
        /// This limit represents the maximum number of requests allowed from guest users 
        /// within the duration of the <see cref="LongTrafficWindow"/>.
        /// A value of <c>null</c> means there is no limit applied for guest users.
        /// </summary>
        public int? LongTrafficLimitGuest { get; set; } = 300;

        /// <summary>
        /// Gets or sets the long traffic limit for bots.
        /// This limit represents the maximum number of requests allowed from bots 
        /// within the duration of the <see cref="LongTrafficWindow"/>.
        /// A value of <c>null</c> means there is no limit applied for bots.
        /// </summary>
        public int? LongTrafficLimitBot { get; set; } = 200;

        /// <summary>
        /// Gets or sets the global traffic limit for both guests and bots together, 
        /// within the duration of the <see cref="LongTrafficWindow"/>.
        /// </summary>
        /// <remarks>
        /// This global limit applies to the combined traffic from guests and bots.
        /// It ensures that the overall system load remains within acceptable thresholds, regardless of the distribution 
        /// of requests among specific user types.
        /// 
        /// Unlike type-specific limits such as <see cref="LongTrafficLimitGuest"/> or <see cref="LongTrafficLimitBot"/>, 
        /// which control the traffic for individual types, this global limit acts as a safeguard for the entire system. 
        /// If the cumulative requests from both types exceed this limit within the observation window, additional requests 
        /// may be denied or throttled, even if individual type-specific limits have not been reached.
        /// 
        /// For example:
        /// - If `LongTrafficLimitGuest` is 600 and `LongTrafficLimitBot` is 400, the system still ensures that no more than 
        ///   `LongTrafficLimitGlobal` (e.g., 800) requests are processed in total.
        /// - If this property is <c>null</c>, no global limit is enforced for the long traffic window.
        /// </remarks>
        public int? LongTrafficLimitGlobal { get; set; } = 500;

        /// <summary>
        /// Gets or sets the duration of the peak traffic observation window.
        /// The peak traffic window defines the shorter period used for detecting sudden traffic spikes.
        /// This setting is useful for reacting to bursts of traffic that occur in a matter of seconds.
        /// </summary>
        public TimeSpan PeakTrafficWindow { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets the peak traffic limit for guest users.
        /// This limit represents the maximum number of requests allowed from guest users 
        /// within the duration of the <see cref="PeakTrafficWindow"/>.
        /// A value of <c>null</c> means there is no limit applied for guest users during peak windows.
        /// </summary>
        public int? PeakTrafficLimitGuest { get; set; } = 30;

        /// <summary>
        /// Gets or sets the peak traffic limit for bots.
        /// This limit represents the maximum number of requests allowed from bots 
        /// within the duration of the <see cref="PeakTrafficWindow"/>.
        /// A value of <c>null</c> means there is no limit applied for bots during peak windows.
        /// </summary>
        public int? PeakTrafficLimitBot { get; set; } = 20;

        /// <summary>
        /// Gets or sets the global peak traffic limit for both guests and bots together, 
        /// within the duration of the <see cref="PeakTrafficWindow"/>.
        /// </summary>
        /// <remarks>
        /// This global limit applies to the combined traffic from guests and bots during short, high-intensity traffic bursts.
        /// It acts as an additional layer of protection for the system to prevent overload during sudden spikes in requests.
        /// 
        /// Type-specific limits, such as <see cref="PeakTrafficLimitGuest"/> and <see cref="PeakTrafficLimitBot"/>, 
        /// are used to control traffic for individual types, but the global peak limit ensures that the total 
        /// number of requests processed during the <see cref="PeakTrafficWindow"/> does not exceed the defined threshold.
        /// 
        /// For example:
        /// - If `PeakTrafficLimitGuest` is 50 and `PeakTrafficLimitBot` is 30, the system still ensures that no more than 
        ///   `PeakTrafficLimitGlobal` (e.g., 60) requests are processed in total during the peak window.
        /// - If this property is <c>null</c>, no global limit is enforced for the peak traffic window.
        /// 
        /// This property is particularly useful in scenarios where the system needs to prioritize overall stability over 
        /// individual user type fairness.
        /// </remarks>
        public int? PeakTrafficLimitGlobal { get; set; } = 50;
    }
}
