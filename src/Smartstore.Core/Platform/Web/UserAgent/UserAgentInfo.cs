#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Parsed user agent info
    /// </summary>
    public readonly struct UserAgentInfo
    {
        internal const string Unknown = "Unknown";
        internal readonly static UserAgentInfo UnknownBot = new(UserAgentType.Bot, Unknown, null, null, null);

        /// <summary>
        /// Creates a new instance of <see cref="UserAgentInfo"/>
        /// </summary>
        public UserAgentInfo(
            UserAgentType type,
            string? name,
            string? version,
            UserAgentPlatform? platform,
            UserAgentDevice? device)
        {
            Type = type;
            Name = name;
            Version = version;
            Platform = platform ?? UserAgentPlatform.UnknownPlatform;
            Device = device ?? UserAgentDevice.UnknownDevice;
        }

        /// <summary>
        /// Creates <see cref="UserAgentInfo"/> for a bot.
        /// </summary>
        internal static UserAgentInfo CreateForBot(string botName, UserAgentPlatform? platform)
            => new(UserAgentType.Bot, botName, null, platform, null);

        /// <summary>
        /// Creates <see cref="UserAgentInfo"/> for a browser.
        /// </summary>
        internal static UserAgentInfo CreateForBrowser(string? browserName, string? browserVersion, UserAgentPlatform? platform, UserAgentDevice? device)
            => new(UserAgentType.Browser, browserName, browserVersion, platform, device);

        /// <summary>
        /// Creates <see cref="UserAgentInfo"/> for an unknown agent type.
        /// </summary>
        internal static UserAgentInfo CreateForUnknown(UserAgentPlatform? platform, UserAgentDevice? device)
            => new(UserAgentType.Unknown, null, null, platform, device);

        /// <summary>
        /// Type of user agent, see <see cref="UserAgentType"/>
        /// </summary>
        public UserAgentType Type { get; }

        /// <summary>
        /// Name of user agent, e.g. "Chrome", "Edge", "Firefox", "Opera Mobile", "Googlebot" etc.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Version of Browser or Bot, e.g. "79.0", "83.0.125.4"
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Platform/OS of user agent, see <see cref="UserAgentPlatform"/>.
        /// </summary>
        public UserAgentPlatform Platform { get; }

        /// <summary>
        /// Device of user agent, see <see cref="UserAgentDevice"/>.
        /// </summary>
        public UserAgentDevice Device { get; }
    }
}
