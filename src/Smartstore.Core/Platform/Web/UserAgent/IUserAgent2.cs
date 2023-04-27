#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Provides parsed and materialized user agent information.
    /// </summary>
    public interface IUserAgent2
    {
        /// <summary>
        /// The raw user agent string.
        /// </summary>
        string RawValue { get; set; }

        /// <summary>
        /// Checks if agent is a bot.
        /// </summary>
        bool IsBot { get; }

        /// <summary>
        /// Checks if agent is a mobile device.
        /// </summary>
        bool IsMobileDevice { get; }

        /// <summary>
        /// Checks if agent is a mobile tablet device.
        /// </summary>
        bool IsTablet { get; }

        /// <summary>
        /// Checks if agent is the PDF converter client.
        /// </summary>
        bool IsPdfConverter { get; }

        /// <summary>
        /// Browser or Bot name of user agent, e.g. "Chrome", "Edge", "Firefox", "Opera Mobile", "Googlebot" etc.
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Version of Browser or Bot.
        /// </summary>
        SemanticVersion Version { get; }

        /// <summary>
        /// Platform/OS of agent, see <see cref="UserAgentPlatform"/>.
        /// </summary>
        UserAgentPlatform? Platform { get; }

        /// <summary>
        /// Mobile device name of agent, e.g. "Android", "Apple iPhone", "BlackBerry", "Samsung", "PlayStation", "Windows CE" etc.
        /// </summary>
        string? MobileDeviceName { get; }
    }
}
