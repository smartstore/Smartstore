using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Smartstore.ComponentModel;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgentParser : IUserAgentParser
    {
        enum UaGroup : byte
        {
            Browser,
            Platform,
            Bot,
            Device
        }
        
        const string GenericBot = "Generic bot";

        private List<UaMatcher> _browsers = new();
        private List<UaMatcher> _platforms = new();
        private List<UaMatcher> _bots = new();
        private List<UaMatcher> _devices = new();

        private readonly ReaderWriterLockSlim _rwLock = new();
        private readonly IApplicationContext _appContext;

        public DefaultUserAgentParser(IApplicationContext appContext)
        {
            _appContext = appContext;
            //ReadMappings();
        }

        #region YAML

        private void ReadMappings()
        {
            // Read YAML from file or embedded resource
            var yaml = ReadYaml();

            // Parse YAML content to YAML mappings
            var mappings = ParseYaml(yaml);

            // Create matchers for browsers
            ConvertMapping(mappings.Get("browsers"), _browsers, UaGroup.Browser);
            // Create matchers for platforms
            ConvertMapping(mappings.Get("platforms"), _platforms, UaGroup.Platform);
            // Create matchers for bots
            ConvertMapping(mappings.Get("bots"), _bots, UaGroup.Bot);
            // Create matchers for devices
            ConvertMapping(mappings.Get("devices"), _devices, UaGroup.Device);
        }

        private string ReadYaml()
        {
            var physicalFile = _appContext.AppDataRoot.GetFile("useragent.yml");
            if (physicalFile.Exists)
            {
                return physicalFile.ReadAllText();
            }

            using var stream = Assembly
                .GetEntryAssembly()
                .GetManifestResourceStream("Smartstore.Web.App_Data.useragent.yml");

            return stream.AsString();
        }

        private static IDictionary<string, YamlMapping> ParseYaml(string yaml)
        {
            var parser = new MinimalYamlParser(yaml);
            return parser.Mappings;
        }

        private static void ConvertMapping(YamlMapping mapping, List<UaMatcher> target, UaGroup group)
        {
            target.Clear();

            var isRegexMatch = group is UaGroup.Browser or UaGroup.Platform;
            var hasPlatform = group is UaGroup.Platform;
            var regexFlags = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline;

            for (var i = 0; i < mapping.Sequences.Count; i++) 
            {
                var sequence = mapping.Sequences[i];
                
                if (!sequence.TryGetValue("match", out var match))
                {
                    continue;
                }

                UaMatcher matcher;
                if (isRegexMatch)
                {
                    var regex = new Regex(match, regexFlags);
                    matcher = new RegexMatcher(regex);
                }
                else
                {
                    matcher = new ContainsMatcher(match);
                }

                matcher.Name = SeekMapping("name", i) ?? match;

                if (hasPlatform)
                {
                    var platformStr = SeekMapping("family", i);
                    if (platformStr.HasValue() && Enum.TryParse<UserAgentPlatformFamily>(platformStr, out var family))
                    {
                        matcher.Platform = family;
                    }
                    else
                    {
                        matcher.Platform = UserAgentPlatformFamily.Generic;
                    }
                }

                target.Add(matcher);
            }

            string SeekMapping(string name, int startIndex)
            {
                for (var i = startIndex; i < mapping.Sequences.Count; i++)
                {
                    if (mapping.Sequences[i].TryGetValue(name, out var value))
                    {
                        return value;
                    }
                }

                return null;
            }
        }

        #endregion

        #region UserAgent

        public UserAgentInfo Parse(string userAgent)
        {
            userAgent = userAgent.TrimSafe();

            if (userAgent.IsEmpty())
            {
                // Empty useragent > bad bot!
                return UserAgentInfo.UnknownBot;
            }

            // Analyze Bot
            if (TryGetBot(userAgent!, out string botName))
            {
                return UserAgentInfo.CreateForBot(botName, GetPlatform(userAgent!));
            }

            // Analyze Platform
            var platform = GetPlatform(userAgent!);

            // Analyze device
            var device = GetDevice(userAgent!);

            // Analyze Browser
            if (TryGetBrowser(userAgent, out (string Name, string Version)? browser))
            {
                var type = browser?.Name is "Smartstore" ? UserAgentType.Application : UserAgentType.Browser;
                return new UserAgentInfo(type, browser?.Name, browser?.Version, platform, device);
            }
            else
            {
                if (userAgent!.ContainsNoCase("bot") && !userAgent!.ContainsNoCase("cubot"))
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
        private static bool TryGetPlatform(string userAgent, out UserAgentPlatform? platform)
        {
            platform = GetPlatform(userAgent);
            return platform is not null;
        }

        /// <summary>
        /// returns the browser or null
        /// </summary>
        private static (string Name, string Version)? GetBrowser(string userAgent)
        {
            foreach ((Regex key, string value) in UserAgentPatterns.Browsers)
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
        public static bool TryGetBrowser(string userAgent, [NotNullWhen(true)] out (string Name, string Version)? browser)
        {
            browser = GetBrowser(userAgent);
            return browser is not null;
        }

        /// <summary>
        /// Returns the robot or null
        /// </summary>
        private static string GetBot(string userAgent)
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
        public static bool TryGetBot(string userAgent, out string robotName)
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

        #endregion
    }
}
