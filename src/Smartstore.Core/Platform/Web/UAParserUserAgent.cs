using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Smartstore.Utilities;
using uap = UAParser;

namespace Smartstore.Core.Web
{
    public partial class UAParserUserAgent : IUserAgent
    {
        [GeneratedRegex("iPad|Kindle Fire|Nexus 10|Xoom|Transformer|MI PAD|IdeaTab", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex TabletPatternRegex();

        private readonly static uap.Parser _uap;
        private static readonly Regex _tabletPattern = TabletPatternRegex();

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

        private readonly IHttpContextAccessor _httpContextAccessor;

        private string _rawValue;
        private UserAgentInfo _userAgent;
        private DeviceInfo _device;
        private OSInfo _os;

        private bool? _isBot;
        private bool? _isMobileDevice;
        private bool? _isTablet;
        private bool? _isPdfConverter;

        static UAParserUserAgent()
        {
            var path = CommonHelper.MapPath("/App_Data/UAParser.regexes.yaml");

            if (File.Exists(path))
            {
                try
                {
                    _uap = uap.Parser.FromYaml(File.ReadAllText(path));
                    return;
                }
                catch
                {
                }
            }

            _uap = uap.Parser.GetDefault();
        }

        public UAParserUserAgent(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string RawValue
        {
            get
            {
                _rawValue ??= _httpContextAccessor.HttpContext?.Request?.UserAgent() ?? string.Empty;
                return _rawValue;
            }
            // for (unit) test purpose
            set
            {
                _rawValue = value;
                _userAgent = null;
                _device = null;
                _os = null;
                _isBot = null;
                _isMobileDevice = null;
                _isTablet = null;
                _isPdfConverter = null;
            }
        }

        public virtual UserAgentInfo UserAgent
        {
            get
            {
                if (_userAgent == null)
                {
                    var ua = _uap.ParseUserAgent(RawValue);
                    _userAgent = new UserAgentInfo(ua.Family, ua.Major, ua.Minor, ua.Patch);
                }
                return _userAgent;
            }
        }

        public virtual DeviceInfo Device
        {
            get
            {
                if (_device == null)
                {
                    var d = _uap.ParseDevice(RawValue);
                    _device = new DeviceInfo(d.Family, d.IsSpider());
                }
                return _device;
            }
        }

        public virtual OSInfo OS
        {
            get
            {
                if (_os == null)
                {
                    var os = _uap.ParseOS(RawValue);
                    _os = new OSInfo(os.Family, os.Major, os.Minor, os.Patch, os.PatchMinor);
                }
                return _os;
            }
        }

        public virtual bool IsBot
        {
            get
            {
                if (!_isBot.HasValue)
                {
                    // empty useragent > bad bot!
                    _isBot = RawValue.IsEmpty() || Device.IsBot || UserAgent.IsBot;
                }
                return _isBot.Value;
            }
        }

        public virtual bool IsMobileDevice
        {
            get
            {
                if (!_isMobileDevice.HasValue)
                {
                    _isMobileDevice =
                        _mobileOS.Contains(OS.Family) ||
                        _mobileBrowsers.Contains(UserAgent.Family) ||
                        _mobileDevices.Contains(Device.Family);
                }

                return _isMobileDevice.Value;
            }
        }

        public virtual bool IsTablet
        {
            get
            {
                if (!_isTablet.HasValue)
                {
                    _isTablet = _tabletPattern.IsMatch(Device.Family) || OS.Family == "BlackBerry Tablet OS";
                }

                return _isTablet.Value;
            }
        }

        public virtual bool IsPdfConverter
        {
            get
            {
                if (!_isPdfConverter.HasValue)
                {
                    _isPdfConverter = RawValue.EqualsNoCase("wkhtmltopdf");
                }

                return _isPdfConverter.Value;
            }
        }
    }

    internal static class DeviceExtensions
    {
        internal static bool IsSpider(this UAParser.Device device)
        {
            return device.Family.Equals("Spider", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}