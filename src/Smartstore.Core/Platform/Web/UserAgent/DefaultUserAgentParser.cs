using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.ComponentModel;
using Smartstore.Utilities;

namespace Smartstore.Core.Web;

public class DefaultUserAgentParser : Disposable, IUserAgentParser
{
    const string GenericBot = "Generic bot";
    const string DefaultYamlPath = "App_Data/useragent.yml";
    const string CustomYamlPath = "App_Data/useragent-custom.yml";

    // Single volatile reference; readers snapshot it once per Parse() call — no lock needed.
    private volatile UaSnapshot _snapshot = UaSnapshot.Empty;
    // Used only to serialise rare YAML reload callbacks.
    private readonly object _reloadLock = new();
    private IDisposable _yamlWatcher;
    private readonly ILogger _logger;

    private sealed record UaSnapshot(
        UaMatcher[] Browsers,
        UaMatcher[] Platforms,
        UaMatcher[] Bots,
        UaMatcher[] Devices,
        UaMatcher[] Tablets)
    {
        public static readonly UaSnapshot Empty = new([], [], [], [], []);
    }

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

            _snapshot = new UaSnapshot(
                Browsers: [.. ConvertMapping(mappings.Get("browsers"), UserAgentSegment.Browser)],
                Platforms: [.. ConvertMapping(mappings.Get("platforms"), UserAgentSegment.Platform)],
                Bots: [.. ConvertMapping(mappings.Get("bots"), UserAgentSegment.Bot)],
                Devices: [.. ConvertMapping(mappings.Get("devices"), UserAgentSegment.Device)],
                Tablets: [.. ConvertMapping(mappings.Get("tablets"), UserAgentSegment.Device)]
            );
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
        lock (_reloadLock)
        {
            Thread.Sleep(50);
            ReadMappings();
        }
    }

    protected virtual Stream OpenYamlStream(out IChangeToken changeToken)
    {
        // Always watch both paths so that dropping a file into App_Data at runtime
        // triggers a reload, even if the file did not exist when the watcher was registered.
        changeToken = new CompositeChangeToken([
            CommonHelper.ContentRoot.Watch(CustomYamlPath),
            CommonHelper.ContentRoot.Watch(DefaultYamlPath)
        ]);

        // 1. Explicit user customization always wins.
        var customFile = CommonHelper.ContentRoot.GetFile(CustomYamlPath);
        if (customFile.Exists)
        {
            return customFile.OpenRead();
        }

        var assembly = typeof(IUserAgent).Assembly;

        // 2. Physical default file wins only if it is strictly newer than the assembly,
        //    meaning the user intentionally placed a more recent version (e.g. downloaded
        //    from GitHub). If the assembly is equal or newer, the embedded resource takes
        //    precedence so that app updates are always picked up automatically.
        var physicalFile = CommonHelper.ContentRoot.GetFile(DefaultYamlPath);
        if (physicalFile.Exists)
        {
            var assemblyDate = File.GetLastWriteTimeUtc(assembly.Location);
            if (physicalFile.LastModified.UtcDateTime > assemblyDate)
            {
                return physicalFile.OpenRead();
            }
        }

        // 3. Fall back to the embedded resource (assembly is up-to-date or no physical file).
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

    private static List<UaMatcher> ConvertMapping(YamlMapping mapping, UserAgentSegment segment)
    {
        var result = new List<UaMatcher>();

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

            result.Add(matcher);
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

        return result;
    }

    #endregion

    #region UserAgent

    public IEnumerable<string> GetDetectableAgents(UserAgentSegment segment)
    {
        var snapshot = _snapshot;
        var matchers = segment switch
        {
            UserAgentSegment.Bot => snapshot.Bots,
            UserAgentSegment.Platform => snapshot.Platforms,
            UserAgentSegment.Device => snapshot.Devices,
            _ => snapshot.Browsers
        };

        return matchers.Select(m => m.Name).Distinct().Order();
    }

    public UserAgentInfo Parse(string userAgent)
    {
        var userAgentSpan = userAgent.AsSpan().Trim();

        if (userAgentSpan.IsEmpty)
        {
            // Empty useragent > bad bot!
            return UserAgentInfo.UnknownBot;
        }

        // Analyze Bot
        if (TryGetBot(userAgentSpan, out string botName))
        {
            return UserAgentInfo.CreateForBot(botName, GetPlatform(userAgentSpan));
        }

        // Analyze Platform
        var platform = GetPlatform(userAgentSpan);

        // Analyze device
        var device = GetDevice(userAgentSpan);

        // Analyze Browser
        if (TryGetBrowser(userAgentSpan, out (string Name, string Version)? browser))
        {
            var type = browser?.Name is "Smartstore" ? UserAgentType.Application : UserAgentType.Browser;
            return new UserAgentInfo(type, browser?.Name, browser?.Version, platform, device);
        }
        else
        {
            if (userAgentSpan.ContainsNoCase("bot") && !userAgentSpan.ContainsNoCase("cubot"))
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

    private UserAgentPlatform? GetPlatform(ReadOnlySpan<char> userAgent)
    {
        var platforms = _snapshot.Platforms;
        for (var i = 0; i < platforms.Length; i++)
        {
            var matcher = platforms[i];
            if (matcher.Match(userAgent, out var version))
            {
                return new UserAgentPlatform(matcher.Name, matcher.PlatformFamily ?? UserAgentPlatformFamily.Unknown, version);
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetBrowser(ReadOnlySpan<char> userAgent, [NotNullWhen(true)] out (string Name, string Version)? browser)
    {
        var browsers = _snapshot.Browsers;
        for (var i = 0; i < browsers.Length; i++)
        {
            if (browsers[i].Match(userAgent, out var version))
            {
                browser = (browsers[i].Name, version);
                return true;
            }
        }

        browser = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetBot(ReadOnlySpan<char> userAgent, out string botName)
    {
        var bots = _snapshot.Bots;
        for (var i = 0; i < bots.Length; i++)
        {
            if (bots[i].Match(userAgent, out _))
            {
                botName = bots[i].Name;
                return true;
            }
        }

        botName = null;
        return false;
    }

    private UserAgentDevice? GetDevice(ReadOnlySpan<char> userAgent)
    {
        var devices = _snapshot.Devices;
        var tablets = _snapshot.Tablets;
        for (var i = 0; i < devices.Length; i++)
        {
            if (devices[i].Match(userAgent, out _))
            {
                var isTablet = false;
                for (var j = 0; j < tablets.Length; j++)
                {
                    if (tablets[j].Match(userAgent, out _))
                    {
                        isTablet = true;
                        break;
                    }
                }

                return new UserAgentDevice(devices[i].Name, isTablet ? UserAgentDeviceType.Tablet : UserAgentDeviceType.Smartphone);
            }
        }

        return null;
    }

    #endregion
}