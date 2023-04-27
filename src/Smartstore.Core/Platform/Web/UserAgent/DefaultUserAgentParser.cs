#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgentParser : IUserAgentParser
    {
        const string Unknown = "Unknown";

        public UserAgentInformation Parse(string? userAgent)
        {
            userAgent = userAgent.TrimSafe();

            if (userAgent.IsEmpty())
            {
                // Empty useragent > bad bot!
                return UserAgentInformation.CreateForBot(Unknown);
            }

            // analyze
            if (TryGetBot(userAgent!, out string? botName))
            {
                return UserAgentInformation.CreateForBot(botName);
            }

            UserAgentPlatform? platform = GetPlatform(userAgent!);
            string? mobileDeviceType = GetMobileDevice(userAgent!);

            if (TryGetBrowser(userAgent!, out (string Name, string? Version)? browser))
            {
                return UserAgentInformation.CreateForBrowser(platform, browser?.Name, browser?.Version, mobileDeviceType);
            }

            return UserAgentInformation.CreateForUnknown(platform, mobileDeviceType);
        }

        /// <summary>
        /// Returns the platform or null
        /// </summary>
        public static UserAgentPlatform? GetPlatform(string userAgent)
        {
            foreach (var item in UserAgentPatterns.Platforms)
            {
                if (item.Regex.IsMatch(userAgent))
                {
                    return new UserAgentPlatform(item.Name, item.Family);
                }
            }

            return new UserAgentPlatform("Unknown", UserAgentPlatformFamily.Unknown);
        }

        /// <summary>
        /// returns true if platform was found
        /// </summary>
        public static bool TryGetPlatform(string userAgent, [NotNullWhen(true)] out UserAgentPlatform? platform)
        {
            platform = GetPlatform(userAgent);
            return platform is not null;
        }

        /// <summary>
        /// returns the browser or null
        /// </summary>
        public static (string Name, string? Version)? GetBrowser(string userAgent)
        {
            foreach ((Regex key, string? value) in UserAgentPatterns.Browsers)
            {
                Match match = key.Match(userAgent);
                if (match.Success)
                {
                    return (value, match.Groups[1].Value);
                }
            }

            return null;
        }

        /// <summary>
        /// returns true if browser was found
        /// </summary>
        public static bool TryGetBrowser(string userAgent, [NotNullWhen(true)] out (string Name, string? Version)? browser)
        {
            browser = GetBrowser(userAgent);
            return browser is not null;
        }

        /// <summary>
        /// returns the robot or null
        /// </summary>
        public static string? GetBot(string userAgent)
        {
            foreach ((string key, string value) in UserAgentPatterns.Robots)
            {
                if (userAgent.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if robot was found
        /// </summary>
        public static bool TryGetBot(string userAgent, [NotNullWhen(true)] out string? robotName)
        {
            robotName = GetBot(userAgent);
            return robotName is not null;
        }

        /// <summary>
        /// returns the device or null
        /// </summary>
        public static string? GetMobileDevice(string userAgent)
        {
            foreach ((string key, string value) in UserAgentPatterns.Mobiles)
            {
                if (userAgent.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// returns true if device was found
        /// </summary>
        public static bool TryGetMobileDevice(string userAgent, [NotNullWhen(true)] out string? device)
        {
            device = GetMobileDevice(userAgent);
            return device is not null;
        }
    }
}
