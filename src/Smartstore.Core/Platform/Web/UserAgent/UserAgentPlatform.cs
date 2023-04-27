namespace Smartstore.Core.Web
{
    /// <summary>
    /// User agent platform info.
    /// </summary>
    public readonly struct UserAgentPlatform
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserAgentPlatform"/>
        /// </summary>
        public UserAgentPlatform(string name, UserAgentPlatformFamily family)
        {
            Name = name;
            Family = family;
        }

        /// <summary>
        /// Platform/OS name of user agent, e.g. "Windows", "Android", "Linux", "iOS" etc.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Platform/OS family.
        /// </summary>
        public UserAgentPlatformFamily Family { get; }
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