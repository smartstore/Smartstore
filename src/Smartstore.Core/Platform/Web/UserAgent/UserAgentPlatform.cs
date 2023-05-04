#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// User agent platform info.
    /// </summary>
    public readonly struct UserAgentPlatform
    {
        internal readonly static UserAgentPlatform UnknownPlatform = new("Unknown", UserAgentPlatformFamily.Unknown);

        /// <summary>
        /// Creates a new instance of <see cref="UserAgentPlatform"/>
        /// </summary>
        public UserAgentPlatform(string name, UserAgentPlatformFamily family, string? version = null)
        {
            Name = name;
            Family = family;
            Version = version;
        }

        /// <summary>
        /// Platform/OS name of user agent, e.g. "Windows", "Android", "Linux", "iOS" etc.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Platform/OS family.
        /// </summary>
        public UserAgentPlatformFamily Family { get; }

        /// <summary>
        /// For future use.
        /// </summary>
        public string? Version { get; }

        public bool IsUnknown() => Name == "Unknown";
    }

    /// <summary>
    /// User Agent platform types
    /// </summary>
    public enum UserAgentPlatformFamily : byte
    {
        /// <summary>
        /// Unknown / not mapped
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Generic
        /// </summary>
        Generic,
        /// <summary>
        /// Windows
        /// </summary>
        Windows,
        /// <summary>
        /// Linux
        /// </summary>
        Linux,
        /// <summary>
        /// Unix
        /// </summary>
        Unix,
        /// <summary>
        /// Apple iOS
        /// </summary>
        IOS,
        /// <summary>
        /// MacOS
        /// </summary>
        MacOS,
        /// <summary>
        /// BlackBerry
        /// </summary>
        BlackBerry,
        /// <summary>
        /// Android
        /// </summary>
        Android,
        /// <summary>
        /// Symbian
        /// </summary>
        Symbian
    }
}