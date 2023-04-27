using System.Text.RegularExpressions;

namespace Smartstore.Core.Web
{
    public readonly struct UserAgentPlatformInfo
    {
        public UserAgentPlatformInfo(Regex regex, string name, UserAgentPlatformFamily family)
        {
            Regex = regex;
            Name = name;
            Family = family;
        }

        public Regex Regex { get; }
        public string Name { get; }
        public UserAgentPlatformFamily Family { get; }
    }

    /// <summary>
    /// Parser settings
    /// </summary>
    public static class UserAgentPatterns
    {
        /// <summary>
        /// Regex defauls for platform mappings
        /// </summary>
        private const RegexOptions DefaultPlatformsRegexFlags = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        /// <summary>
        /// Creates default platform mapping regex
        /// </summary>
        private static Regex CreateDefaultPlatformRegex(string key) => new(Regex.Escape($"{key}"), DefaultPlatformsRegexFlags);

        /// <summary>
        /// Platforms
        /// </summary>
        public static readonly HashSet<UserAgentPlatformInfo> Platforms = new()
        {
            new(CreateDefaultPlatformRegex("windows nt 10.0"), "Windows 10", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 6.3"), "Windows 8.1", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 6.2"), "Windows 8", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 6.1"), "Windows 7", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 6.0"), "Windows Vista", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 5.2"), "Windows 2003", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 5.1"), "Windows XP", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 5.0"), "Windows 2000", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows nt 4.0"), "Windows NT 4.0", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("winnt4.0"), "Windows NT 4.0", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("winnt 4.0"), "Windows NT", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("winnt"), "Windows NT", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows 98"), "Windows 98", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("win98"), "Windows 98", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows 95"), "Windows 95", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("win95"), "Windows 95", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows phone"), "Windows Phone", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("windows"), "Unknown Windows OS", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("android"), "Android", UserAgentPlatformFamily.Android),
            new(CreateDefaultPlatformRegex("blackberry"), "BlackBerry", UserAgentPlatformFamily.BlackBerry),
            new(CreateDefaultPlatformRegex("iphone"), "iOS", UserAgentPlatformFamily.IOS),
            new(CreateDefaultPlatformRegex("ipad"), "iOS", UserAgentPlatformFamily.IOS),
            new(CreateDefaultPlatformRegex("ipod"), "iOS", UserAgentPlatformFamily.IOS),
            new(CreateDefaultPlatformRegex("os x"), "Mac OS X", UserAgentPlatformFamily.MacOS),
            new(CreateDefaultPlatformRegex("ppc mac"), "Power PC Mac", UserAgentPlatformFamily.MacOS),
            new(CreateDefaultPlatformRegex("freebsd"), "FreeBSD", UserAgentPlatformFamily.Linux),
            new(CreateDefaultPlatformRegex("ppc"), "Macintosh", UserAgentPlatformFamily.Linux),
            new(CreateDefaultPlatformRegex("linux"), "Linux", UserAgentPlatformFamily.Linux),
            new(CreateDefaultPlatformRegex("debian"), "Debian", UserAgentPlatformFamily.Linux),
            new(CreateDefaultPlatformRegex("sunos"), "Sun Solaris", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("beos"), "BeOS", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("apachebench"), "ApacheBench", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("aix"), "AIX", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("irix"), "Irix", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("osf"), "DEC OSF", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("hp-ux"), "HP-UX", UserAgentPlatformFamily.Windows),
            new(CreateDefaultPlatformRegex("netbsd"), "NetBSD", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("bsdi"), "BSDi", UserAgentPlatformFamily.Generic),
            new(CreateDefaultPlatformRegex("openbsd"), "OpenBSD", UserAgentPlatformFamily.Unix),
            new(CreateDefaultPlatformRegex("gnu"), "GNU/Linux", UserAgentPlatformFamily.Linux),
            new(CreateDefaultPlatformRegex("unix"), "Unknown Unix OS", UserAgentPlatformFamily.Unix),
            new(CreateDefaultPlatformRegex("symbian"), "Symbian OS", UserAgentPlatformFamily.Symbian),
        };

        /// <summary>
        /// Regex defauls for browser mappings
        /// </summary>
        private const RegexOptions DefaultBrowserRegexFlags = RegexOptions.IgnoreCase | RegexOptions.Compiled;
        /// <summary>
        /// Creates default browser mapping regex
        /// </summary>
        private static Regex CreateDefaultBrowserRegex(string key) => new($@"{key}.*?([0-9\.]+)", DefaultBrowserRegexFlags);

        /// <summary>
        /// Browsers
        /// </summary>
        public static Dictionary<Regex, string> Browsers = new()
        {
            { CreateDefaultBrowserRegex("OPR"), "Opera" },
            { CreateDefaultBrowserRegex("Flock"), "Flock" },
            { CreateDefaultBrowserRegex("Edge"), "Edge" },
            { CreateDefaultBrowserRegex("EdgA"), "Edge" },
            { CreateDefaultBrowserRegex("Edg"), "Edge" },
            { CreateDefaultBrowserRegex("Vivaldi"), "Vivaldi" },
            { CreateDefaultBrowserRegex("Brave Chrome"), "Brave" },
            { CreateDefaultBrowserRegex("Chrome"), "Chrome" },
            { CreateDefaultBrowserRegex("CriOS"), "Chrome" },
            { CreateDefaultBrowserRegex("Opera.*?Version"), "Opera" },
            { CreateDefaultBrowserRegex("Opera"), "Opera" },
            { CreateDefaultBrowserRegex("MSIE"), "Internet Explorer" },
            { CreateDefaultBrowserRegex("Internet Explorer"), "Internet Explorer" },
            { CreateDefaultBrowserRegex("Trident.* rv"), "Internet Explorer" },
            { CreateDefaultBrowserRegex("Shiira"), "Shiira" },
            { CreateDefaultBrowserRegex("Firefox"), "Firefox" },
            { CreateDefaultBrowserRegex("FxiOS"), "Firefox" },
            { CreateDefaultBrowserRegex("Chimera"), "Chimera" },
            { CreateDefaultBrowserRegex("Phoenix"), "Phoenix" },
            { CreateDefaultBrowserRegex("Firebird"), "Firebird" },
            { CreateDefaultBrowserRegex("Camino"), "Camino" },
            { CreateDefaultBrowserRegex("Netscape"), "Netscape" },
            { CreateDefaultBrowserRegex("OmniWeb"), "OmniWeb" },
            { CreateDefaultBrowserRegex("Safari"), "Safari" },
            { CreateDefaultBrowserRegex("Mozilla"), "Mozilla" },
            { CreateDefaultBrowserRegex("Konqueror"), "Konqueror" },
            { CreateDefaultBrowserRegex("icab"), "iCab" },
            { CreateDefaultBrowserRegex("Lynx"), "Lynx" },
            { CreateDefaultBrowserRegex("Links"), "Links" },
            { CreateDefaultBrowserRegex("hotjava"), "HotJava" },
            { CreateDefaultBrowserRegex("amaya"), "Amaya" },
            { CreateDefaultBrowserRegex("IBrowse"), "IBrowse" },
            { CreateDefaultBrowserRegex("Maxthon"), "Maxthon" },
            { CreateDefaultBrowserRegex("ipod touch"), "Apple iPod" },
            { CreateDefaultBrowserRegex("Ubuntu"), "Ubuntu Web Browser" },
        };

        /// <summary>
        /// Mobiles
        /// </summary>
        public static readonly Dictionary<string, string> Mobiles = new()
        {
            // Legacy
            { "mobileexplorer", "Mobile Explorer" },
            { "palmsource", "Palm" },
            { "palmscape", "Palmscape" },
            // Phones and Manufacturers
            { "motorola", "Motorola" },
            { "nokia", "Nokia" },
            { "palm", "Palm" },
            { "ipad", "Apple iPad" },
            { "ipod", "Apple iPod" },
            { "iphone", "Apple iPhone" },
            { "sony", "Sony Ericsson" },
            { "ericsson", "Sony Ericsson" },
            { "blackberry", "BlackBerry" },
            { "cocoon", "O2 Cocoon" },
            { "blazer", "Treo" },
            { "lg", "LG" },
            { "amoi", "Amoi" },
            { "xda", "XDA" },
            { "mda", "MDA" },
            { "vario", "Vario" },
            { "htc", "HTC" },
            { "samsung", "Samsung" },
            { "sharp", "Sharp" },
            { "sie-", "Siemens" },
            { "alcatel", "Alcatel" },
            { "benq", "BenQ" },
            { "ipaq", "HP iPaq" },
            { "mot-", "Motorola" },
            { "playstation portable", "PlayStation Portable" },
            { "playstation 3", "PlayStation 3" },
            { "playstation vita", "PlayStation Vita" },
            { "hiptop", "Danger Hiptop" },
            { "nec-", "NEC" },
            { "panasonic", "Panasonic" },
            { "philips", "Philips" },
            { "sagem", "Sagem" },
            { "sanyo", "Sanyo" },
            { "spv", "SPV" },
            { "zte", "ZTE" },
            { "sendo", "Sendo" },
            { "nintendo dsi", "Nintendo DSi" },
            { "nintendo ds", "Nintendo DS" },
            { "nintendo 3ds", "Nintendo 3DS" },
            { "wii", "Nintendo Wii" },
            { "open web", "Open Web" },
            { "openweb", "OpenWeb" },
            // Operating Systems
            { "android", "Android" },
            { "symbian", "Symbian" },
            { "SymbianOS", "SymbianOS" },
            { "elaine", "Palm" },
            { "series60", "Symbian S60" },
            { "windows ce", "Windows CE" },
            // Browsers
            { "obigo", "Obigo" },
            { "netfront", "Netfront Browser" },
            { "openwave", "Openwave Browser" },
            { "mobilexplorer", "Mobile Explorer" },
            { "operamini", "Opera Mini" },
            { "opera mini", "Opera Mini" },
            { "opera mobi", "Opera Mobile" },
            { "fennec", "Firefox Mobile" },
            // Other
            { "digital paths", "Digital Paths" },
            { "avantgo", "AvantGo" },
            { "xiino", "Xiino" },
            { "novarra", "Novarra Transcoder" },
            { "vodafone", "Vodafone" },
            { "docomo", "NTT DoCoMo" },
            { "o2", "O2" },
            // Fallback
            { "mobile", "Generic Mobile" },
            { "wireless", "Generic Mobile" },
            { "j2me", "Generic Mobile" },
            { "midp", "Generic Mobile" },
            { "cldc", "Generic Mobile" },
            { "up.link", "Generic Mobile" },
            { "up.browser", "Generic Mobile" },
            { "smartphone", "Generic Mobile" },
            { "cellphone", "Generic Mobile" },
        };

        /// <summary>
        /// Robots
        /// </summary>
        public static readonly (string Key, string Value)[] Robots =
        {
            ( "googlebot", "Googlebot" ),
            ( "googleweblight", "Google Web Light" ),
            ( "PetalBot", "PetalBot"),
            ( "DuplexWeb-Google", "DuplexWeb-Google"),
            ( "Storebot-Google", "Storebot-Google"),
            ( "msnbot", "MSNBot"),
            ( "baiduspider", "Baiduspider"),
            ( "Google Favicon", "Google Favicon"),
            ( "Jobboerse", "Jobboerse"),
            ( "bingbot", "BingBot"),
            ( "BingPreview", "Bing Preview"),
            ( "slurp", "Slurp"),
            ( "yahoo", "Yahoo"),
            ( "ask jeeves", "Ask Jeeves"),
            ( "fastcrawler", "FastCrawler"),
            ( "infoseek", "InfoSeek Robot 1.0"),
            ( "lycos", "Lycos"),
            ( "YandexBot", "YandexBot"),
            ( "YandexImages", "YandexImages"),
            ( "mediapartners-google", "Mediapartners Google"),
            ( "apis-google", "APIs Google"),
            ( "CRAZYWEBCRAWLER", "Crazy Webcrawler"),
            ( "AdsBot-Google-Mobile", "AdsBot Google Mobile"),
            ( "adsbot-google", "AdsBot Google"),
            ( "feedfetcher-google", "FeedFetcher-Google"),
            ( "google-read-aloud", "Google-Read-Aloud"),
            ( "curious george", "Curious George"),
            ( "ia_archiver", "Alexa Crawler"),
            ( "MJ12bot", "Majestic"),
            ( "Uptimebot", "Uptimebot"),
            ( "CheckMarkNetwork", "CheckMark"),
            ( "facebookexternalhit", "Facebook"),
            ( "adscanner", "AdScanner"),
            ( "AhrefsBot", "Ahrefs"),
            ( "BLEXBot", "BLEXBot"),
            ( "DotBot", "OpenSite"),
            ( "Mail.RU_Bot", "Mail.ru"),
            ( "MegaIndex", "MegaIndex"),
            ( "SemrushBot", "SEMRush"),
            ( "SEOkicks", "SEOkicks"),
            ( "seoscanners.net", "SEO Scanners"),
            ( "Sistrix", "Sistrix" )
        };

        /// <summary>
        /// Tools
        /// </summary>
        public static readonly Dictionary<string, string> Tools = new()
        {
            { "curl", "curl" }
        };
    }
}