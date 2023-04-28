#nullable enable

using Smartstore.Utilities;
using UAParser;

namespace Smartstore.Core.Web
{
    public class UAUserAgentParser : IUserAgentParser
    {
        const string Other = "Other";

        private readonly UAParser.Parser _uap;

        #region Mobile UAs, OS & Devices

        private static readonly HashSet<string> _mobileOS = new(StringComparer.InvariantCultureIgnoreCase)
        {
            "Android",
            "iOS",
            "Windows Mobile",
            "Windows Phone",
            "Windows CE",
            "Symbian OS",
            "BlackBerry OS",
            "BlackBerry Tablet OS",
            "Firefox OS",
            "Brew MP",
            "webOS",
            "Bada",
            "Kindle",
            "Maemo"
        };

        private static readonly HashSet<string> _mobileBrowsers = new(StringComparer.InvariantCultureIgnoreCase)
        {
            "Googlebot-Mobile",
            "Baiduspider-mobile",
            "Android",
            "Firefox Mobile",
            "Opera Mobile",
            "Opera Mini",
            "Mobile Safari",
            "Amazon Silk",
            "webOS Browser",
            "MicroB",
            "Ovi Browser",
            "NetFront",
            "NetFront NX",
            "Chrome Mobile",
            "Chrome Mobile iOS",
            "UC Browser",
            "Tizen Browser",
            "Baidu Explorer",
            "QQ Browser Mini",
            "QQ Browser Mobile",
            "IE Mobile",
            "Polaris",
            "ONE Browser",
            "iBrowser Mini",
            "Nokia Services (WAP) Browser",
            "Nokia Browser",
            "Nokia OSS Browser",
            "BlackBerry WebKit",
            "BlackBerry", "Palm",
            "Palm Blazer",
            "Palm Pre",
            "Teleca Browser",
            "SEMC-Browser",
            "PlayStation Portable",
            "Nokia",
            "Maemo Browser",
            "Obigo",
            "Bolt",
            "Iris",
            "UP.Browser",
            "Minimo",
            "Bunjaloo",
            "Jasmine",
            "Dolfin",
            "Polaris",
            "Skyfire"
        };

        private static readonly HashSet<string> _mobileDevices = new(StringComparer.InvariantCultureIgnoreCase)
        {
            "BlackBerry",
            "MI PAD",
            "iPhone",
            "iPad",
            "iPod",
            "Kindle",
            "Kindle Fire",
            "Nokia",
            "Lumia",
            "Palm",
            "DoCoMo",
            "HP TouchPad",
            "Xoom",
            "Motorola",
            "Generic Feature Phone",
            "Generic Smartphone"
        };

        #endregion

        public UAUserAgentParser()
        {
            var path = CommonHelper.MapPath("/App_Data/UAParser.regexes.yaml");

            if (File.Exists(path))
            {
                try
                {
                    _uap = UAParser.Parser.FromYaml(File.ReadAllText(path));
                    return;
                }
                catch
                {
                }
            }

            _uap = UAParser.Parser.GetDefault();
        }

        public UserAgentInformation Parse(string? userAgent)
        {
            if (userAgent.IsEmpty())
            {
                // Empty useragent > bad bot!
                return UserAgentInformation.UnknownBot;
            }

            var info = _uap.Parse(userAgent);
            
            return new UserAgentInformation(
                type: GetType(info), 
                name: GetName(info), 
                version: GetVersion(info), 
                platform: GetPlatform(info), 
                device: GetDevice(info));
        }

        private static UserAgentType GetType(ClientInfo info)
        {
            if (info.Device.IsSpider)
            {
                return UserAgentType.Bot;
            }

            return UserAgentType.Browser;
        }

        private static string? GetName(ClientInfo info)
        {
            return info.UA.Family;
        }

        private static string? GetVersion(ClientInfo info)
        {
            var ua = info.UA;
            return VersionString.Format(ua.Major, ua.Minor, ua.Patch).NullEmpty();
        }

        private static UserAgentPlatform? GetPlatform(ClientInfo info)
        {
            var family = info.OS.Family;
            UserAgentPlatformFamily platformFamily = UserAgentPlatformFamily.Unknown;

            if (family.StartsWith("Android"))
            {
                platformFamily = UserAgentPlatformFamily.Android;
            }
            else if (family.StartsWith("BlackBerry"))
            {
                platformFamily = UserAgentPlatformFamily.BlackBerry;
            }
            else if (family.StartsWith("iOS"))
            {
                platformFamily = UserAgentPlatformFamily.IOS;
            }
            else if (family.StartsWith("Linux"))
            {
                platformFamily = UserAgentPlatformFamily.Linux;
            }
            else if (family.StartsWith("Mac"))
            {
                platformFamily = UserAgentPlatformFamily.MacOS;
            }
            else if (family.StartsWith("Symbian"))
            {
                platformFamily = UserAgentPlatformFamily.Symbian;
            }
            else if (family.StartsWith("Windows"))
            {
                platformFamily = UserAgentPlatformFamily.Windows;
            }
            else if (family != Other)
            {
                platformFamily = UserAgentPlatformFamily.Generic;
            }

            return new UserAgentPlatform(family, platformFamily);
        }

        private static UserAgentDevice? GetDevice(ClientInfo info)
        {
            if (_mobileOS.Contains(info.OS.Family))
            {
                return new UserAgentDevice(info.OS.Family, UserAgentDeviceType.Smartphone);
            }

            if (_mobileBrowsers.Contains(info.UA.Family))
            {
                return new UserAgentDevice(info.UA.Family, UserAgentDeviceType.Smartphone);
            }

            if (_mobileDevices.Contains(info.Device.Family))
            {
                return new UserAgentDevice(info.Device.Family, UserAgentDeviceType.Smartphone);
            }

            return null;
        }
    }
}
