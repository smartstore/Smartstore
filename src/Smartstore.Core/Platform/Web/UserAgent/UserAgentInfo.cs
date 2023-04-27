#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Parsed user agent info
    /// </summary>
    public readonly struct UserAgentInformation
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserAgentInformation"/>
        /// </summary>
        public UserAgentInformation(
            UserAgentType type,
            string? name,
            string? version,
            UserAgentPlatform? platform,
            string? mobileDeviceName)
        {
            Type = type;
            Name = name;
            Version = version;
            Platform = platform;
            MobileDeviceName = mobileDeviceName;
        }

        /// <summary>
        /// Creates <see cref="UserAgentInformation"/> for a bot.
        /// </summary>
        internal static UserAgentInformation CreateForBot(string botName)
            => new(UserAgentType.Bot, botName, null, null, null);

        /// <summary>
        /// Creates <see cref="UserAgentInformation"/> for a browser.
        /// </summary>
        internal static UserAgentInformation CreateForBrowser(UserAgentPlatform? platform, string? browserName, string? browserVersion, string? deviceName)
            => new(UserAgentType.Browser, browserName, browserVersion, platform, deviceName);

        /// <summary>
        /// Creates <see cref="UserAgentInformation"/> for an unknown agent type.
        /// </summary>
        internal static UserAgentInformation CreateForUnknown(UserAgentPlatform? platform, string? deviceName)
            => new(UserAgentType.Unknown, null, null, platform, deviceName);

        /// <summary>
        /// Type of user agent, see <see cref="UserAgentType"/>
        /// </summary>
        public UserAgentType Type { get; }

        /// <summary>
        /// Browser or Bot name of user agent, e.g. "Chrome", "Edge", "Firefox", "Opera Mobile", "Googlebot" etc.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Version of Browser or Bot, e.g. "79.0", "83.0.125.4"
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Platform/OS of user agent, see <see cref="UserAgentPlatform"/>.
        /// </summary>
        public UserAgentPlatform? Platform { get; }

        /// <summary>
        /// Mobile device name of user agent, e.g. "Android", "Apple iPhone", "BlackBerry", "Samsung", "PlayStation", "Windows CE" etc.
        /// </summary>
        public string? MobileDeviceName { get; }
    }

    public static class UserAgentInfoExtensions
    {
        /// <summary>
        /// Tests if <paramref name="userAgent"/> is of <paramref name="type" />.
        /// </summary>
        public static bool IsType(this in UserAgentInformation userAgent, UserAgentType type) => userAgent.Type == type;

        /// <summary>
        /// Tests if <paramref name="userAgent"/> is of type <see cref="UserAgentType.Bot"/>.
        /// </summary>
        public static bool IsBot(this in UserAgentInformation userAgent) => userAgent.Type == UserAgentType.Bot;

        /// <summary>
        /// Tests if <paramref name="userAgent"/> is of type <see cref="UserAgentType.Browser"/>.
        /// </summary>
        public static bool IsBrowser(this in UserAgentInformation userAgent) => userAgent.Type == UserAgentType.Browser;

        /// <summary>
        /// returns <c>true</c> if agent is a mobile device.
        /// </summary>
        /// <remarks>checks if <see cref="UserAgentInformation.MobileDeviceName"/> is null</remarks>
        public static bool IsMobile(this in UserAgentInformation userAgent) => userAgent.MobileDeviceName is not null;
    }
}
