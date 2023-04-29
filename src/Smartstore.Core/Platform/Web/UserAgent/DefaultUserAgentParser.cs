#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgentParser : IUserAgentParser
    {
        const string GenericBot = "Generic bot";
        
        public UserAgentInfo Parse(string? userAgent)
        {
            userAgent = userAgent.TrimSafe();

            if (userAgent.IsEmpty())
            {
                // Empty useragent > bad bot!
                return UserAgentInfo.UnknownBot;
            }

            // Analyze Bot
            if (TryGetBot(userAgent!, out string? botName))
            {
                return UserAgentInfo.CreateForBot(botName, GetPlatform(userAgent!));
            }

            // Analyze Platform
            var platform = GetPlatform(userAgent!);

            // Analyze device
            var device = GetDevice(userAgent!);

            // Analyze Browser
            if (TryGetBrowser(userAgent!, out (string Name, string? Version)? browser))
            {
                var type = browser?.Name is "Smartstore" ? UserAgentType.Application : UserAgentType.Browser;
                return new UserAgentInfo(type, browser?.Name, browser?.Version, platform, device);
            }
            else
            {
                if (userAgent!.ContainsNoCase("bot"))
                {
                    // No bot or browser detected. Just check if "bot" is
                    // contained within agent string and simply assume that it's a bot.
                    return UserAgentInfo.CreateForBot(GenericBot, platform);
                }
                else
                {
                    return UserAgentInfo.CreateForUnknown(platform, device);
                }
            }
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

            return null;
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
        /// Returns true if browser was found
        /// </summary>
        public static bool TryGetBrowser(string userAgent, [NotNullWhen(true)] out (string Name, string? Version)? browser)
        {
            browser = GetBrowser(userAgent);
            return browser is not null;
        }

        /// <summary>
        /// Returns the robot or null
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
        /// Returns the device or null
        /// </summary>
        public static UserAgentDevice? GetDevice(string userAgent)
        {
            foreach ((string key, string value) in UserAgentPatterns.Mobiles)
            {
                if (userAgent.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    var isTablet = key == "ipad" || UserAgentPatterns.IsTablet(userAgent);
                    return new UserAgentDevice(value, isTablet ? UserAgentDeviceType.Tablet : UserAgentDeviceType.Smartphone);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if device was found
        /// </summary>
        public static bool TryGetDevice(string userAgent, [NotNullWhen(true)] out UserAgentDevice? device)
        {
            device = GetDevice(userAgent);
            return device is not null;
        }
    }
}
