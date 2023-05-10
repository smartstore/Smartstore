#nullable enable

namespace Smartstore.Core.Web
{
    /// <summary>
    /// Responsible for parsing and materializing a user agent string.
    /// </summary>
    public interface IUserAgentParser
    {
        /// <summary>
        /// Enumerates the friendly names of all detectable agents for the given <paramref name="segment"/>.
        /// </summary>
        IEnumerable<string> GetDetectableAgents(UserAgentSegment segment);

        /// <summary>
        /// Parses and materializes the given raw <paramref name="userAgent"/> string.
        /// </summary>
        UserAgentInfo Parse(string? userAgent);
    }

    public enum UserAgentSegment : byte
    {
        Browser,
        Platform,
        Bot,
        Device
    }

    public static class IUserAgentParserExtensions
    {
        public static IEnumerable<string> GetDetectableBrowsers(this IUserAgentParser parser) => parser.GetDetectableAgents(UserAgentSegment.Browser);
        public static IEnumerable<string> GetDetectableBots(this IUserAgentParser parser) => parser.GetDetectableAgents(UserAgentSegment.Bot);
        public static IEnumerable<string> GetDetectableDevices(this IUserAgentParser parser) => parser.GetDetectableAgents(UserAgentSegment.Device);
        public static IEnumerable<string> GetDetectablePlatforms(this IUserAgentParser parser) => parser.GetDetectableAgents(UserAgentSegment.Platform);
    }
}
