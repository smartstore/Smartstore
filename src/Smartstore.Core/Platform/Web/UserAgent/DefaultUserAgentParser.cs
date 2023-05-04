using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives;
using Smartstore.ComponentModel;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Core.Web
{
    public class DefaultUserAgentParser : Disposable, IUserAgentParser
    {
        const string GenericBot = "Generic bot";
        const string DefaultYamlPath = "App_Data/useragent.yml";

        private List<UaMatcher> _browsers = new();
        private List<UaMatcher> _platforms = new();
        private List<UaMatcher> _bots = new();
        private List<UaMatcher> _devices = new();
        private List<UaMatcher> _tablets = new();
        private IDisposable _yamlWatcher;

        private readonly ReaderWriterLockSlim _rwLock = new();
        private readonly ILogger _logger;

        public DefaultUserAgentParser(ILogger logger)
        {
            _logger = logger;

            // Don't lock initial build-up, we don't expect concurrency issues here.
            ReadMappings();
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing && _yamlWatcher != null)
            {
                _yamlWatcher.Dispose();
                _yamlWatcher = null;
            }
        }

        #region YAML

        private void ReadMappings()
        {
            IChangeToken changeToken = null;
            
            try
            {
                // Read YAML from file or embedded resource
                using var yamlStream = OpenYamlStream(out changeToken);

                // Parse YAML content to YAML mappings
                var mappings = ParseYaml(yamlStream);

                // Create matchers for browsers
                ConvertMapping(mappings.Get("browsers"), _browsers, UserAgentSegment.Browser);
                // Create matchers for platforms
                ConvertMapping(mappings.Get("platforms"), _platforms, UserAgentSegment.Platform);
                // Create matchers for bots
                ConvertMapping(mappings.Get("bots"), _bots, UserAgentSegment.Bot);
                // Create matchers for devices
                ConvertMapping(mappings.Get("devices"), _devices, UserAgentSegment.Device);
                // Create matchers for tablets
                ConvertMapping(mappings.Get("tablets"), _tablets, UserAgentSegment.Device);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read User Agent mappings from YAML file.");
            }
            finally
            {
                // Change monitoring
                if (_yamlWatcher != null)
                {
                    _yamlWatcher.Dispose();
                    _yamlWatcher = null;
                }

                if (changeToken != null)
                {
                    _yamlWatcher = changeToken.RegisterChangeCallback(OnYamlChanged, null);
                }
            }
        }

        private void OnYamlChanged(object state)
        {
            // Read and create mappings locked here because concurrency issues may occur here.
            using (_rwLock.GetWriteLock())
            {
                // Breathe
                Thread.Sleep(50);
                // Read
                ReadMappings();
            }
        }

        protected virtual Stream OpenYamlStream(out IChangeToken changeToken)
        {
            changeToken = CommonHelper.ContentRoot.Watch(DefaultYamlPath);

            // First check if physical file exists in App_Data
            var physicalFile = CommonHelper.ContentRoot.GetFile(DefaultYamlPath);
            if (physicalFile.Exists)
            {
                return physicalFile.OpenRead();
            }

            // If physical file does exist, read embedded file from assembly
            var assembly = typeof(IUserAgent).Assembly;
            var fullPath = assembly.GetManifestResourceNames()
                .Where(x => x.EndsWith("useragent.yml"))
                .FirstOrDefault();

            return assembly.GetManifestResourceStream(fullPath);
        }

        private static IDictionary<string, YamlMapping> ParseYaml(Stream yamlStream)
        {
            var yaml = yamlStream.AsString();
            var parser = new MinimalYamlParser(yaml);
            return parser.Mappings;
        }

        private static void ConvertMapping(YamlMapping mapping, List<UaMatcher> target, UserAgentSegment segment)
        {
            target.Clear();

            var hasPlatform = segment is UserAgentSegment.Platform;
            var regexFlags = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline;

            for (var i = 0; i < mapping.Sequences.Count; i++) 
            {
                var sequence = mapping.Sequences[i];
                
                if (!sequence.TryGetValue("match", out var match))
                {
                    continue;
                }

                // Patterns enclosed in '/' are regex patterns
                var isRegexMatch = match.Length > 2 && match[0] == '/' && match[^1] == '/';

                UaMatcher matcher;
                if (isRegexMatch)
                {
                    match = match.Trim('/');
                    var regex = new Regex(match, regexFlags);
                    matcher = new RegexMatcher(regex);
                }
                else
                {
                    matcher = new ContainsMatcher(match);
                }

                var name = FindMapping("name", i);
                if (name == "$1")
                {
                    name = match;
                }
                matcher.Name = name ?? match;

                if (hasPlatform)
                {
                    var platformStr = FindMapping("family", i);
                    if (platformStr.HasValue() && Enum.TryParse<UserAgentPlatformFamily>(platformStr, out var family))
                    {
                        matcher.PlatformFamily = family;
                    }
                    else
                    {
                        matcher.PlatformFamily = UserAgentPlatformFamily.Generic;
                    }
                }

                target.Add(matcher);
            }

            string FindMapping(string name, int startIndex)
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

        public IEnumerable<string> GetDetectableAgents(UserAgentSegment segment)
        {
            var matchers = segment switch
            {
                UserAgentSegment.Bot => _bots,
                UserAgentSegment.Platform => _platforms,
                UserAgentSegment.Device => _devices,
                _ => _browsers
            };

            return matchers.Select(m => m.Name).Distinct().Order();
        }

        public UserAgentInfo Parse(string userAgent)
        {
            userAgent = userAgent.TrimSafe();

            if (userAgent.IsEmpty())
            {
                // Empty useragent > bad bot!
                return UserAgentInfo.UnknownBot;
            }

            using var locker = _rwLock.GetReadLock();

            // Analyze Bot
            if (TryGetBot(userAgent, out string botName))
            {
                return UserAgentInfo.CreateForBot(botName, GetPlatform(userAgent));
            }

            // Analyze Platform
            var platform = GetPlatform(userAgent);

            // Analyze device
            var device = GetDevice(userAgent);

            // Analyze Browser
            if (TryGetBrowser(userAgent, out (string Name, string Version)? browser))
            {
                var type = browser?.Name is "Smartstore" ? UserAgentType.Application : UserAgentType.Browser;
                return new UserAgentInfo(type, browser?.Name, browser?.Version, platform, device);
            }
            else
            {
                if (userAgent.ContainsNoCase("bot") && !userAgent.ContainsNoCase("cubot"))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetPlatform(string userAgent, out UserAgentPlatform? platform)
        {
            platform = GetPlatform(userAgent);
            return platform is not null;
        }

        private UserAgentPlatform? GetPlatform(string userAgent)
        {
            for (var i = 0; i < _platforms.Count; i++)
            {
                var matcher = _platforms[i];
                if (matcher.Match(userAgent, out var version))
                {
                    return new UserAgentPlatform(matcher.Name, matcher.PlatformFamily ?? UserAgentPlatformFamily.Unknown, version);
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBrowser(string userAgent, [NotNullWhen(true)] out (string Name, string Version)? browser)
        {
            browser = GetBrowser(userAgent);
            return browser is not null;
        }

        private (string Name, string Version)? GetBrowser(string userAgent)
        {
            for (var i = 0; i < _browsers.Count; i++)
            {
                var matcher = _browsers[i];
                if (matcher.Match(userAgent, out var version))
                {
                    return (matcher.Name, version);
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetBot(string userAgent, out string botName)
        {
            botName = GetBot(userAgent);
            return botName is not null;
        }

        private string GetBot(string userAgent)
        {
            for (var i = 0; i < _bots.Count; i++)
            {
                var matcher = _bots[i];
                if (matcher.Match(userAgent, out _))
                {
                    return matcher.Name;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetDevice(string userAgent, [NotNullWhen(true)] out UserAgentDevice? device)
        {
            device = GetDevice(userAgent);
            return device is not null;
        }

        private UserAgentDevice? GetDevice(string userAgent)
        {
            for (var i = 0; i < _devices.Count; i++)
            {
                var matcher = _devices[i];
                if (matcher.Match(userAgent, out _))
                {
                    var isTablet = _tablets.Any(m => m.Match(userAgent, out _));
                    return new UserAgentDevice(matcher.Name, isTablet ? UserAgentDeviceType.Tablet : UserAgentDeviceType.Smartphone);
                }
            }

            return null;
        }

        #endregion
    }
}
