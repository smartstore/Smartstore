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
    public static partial class UserAgentPatterns
    {
        /// <summary>
        /// Regex defauls for platform mappings
        /// </summary>
        private const RegexOptions DefaultPlatformsRegexFlags = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        private static readonly Regex TabletRegex = GeneratedTabletRegex();

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
        /// Creates browser mapping regex that matches Version/[Version]...
        /// </summary>
        private static Regex CreateVersionBrowserRegex(string key) => new($@"Version/([0-9\.]+).*?{key}", DefaultBrowserRegexFlags);

        /// <summary>
        /// Browsers
        /// </summary>
        public static Dictionary<Regex, string> Browsers = new()
        {
            // Common browsers
            { CreateDefaultBrowserRegex("OPIOS/"), "Opera" },
            { CreateDefaultBrowserRegex("OPR/"), "Opera" },
            { CreateDefaultBrowserRegex("OPT/"), "Opera" },
            { CreateDefaultBrowserRegex("Chrome"), "Chrome" },
            { CreateDefaultBrowserRegex("Firefox"), "Firefox" },
            { CreateDefaultBrowserRegex("Opera.*?Version"), "Opera" },
            { CreateDefaultBrowserRegex("Opera"), "Opera" },
            { CreateVersionBrowserRegex("Safari"), "Safari" },
            { CreateDefaultBrowserRegex("Safari"), "Safari" },
            { CreateDefaultBrowserRegex("Edge"), "Edge" },
            { CreateDefaultBrowserRegex("EdgA"), "Edge" },
            { CreateDefaultBrowserRegex("Edg"), "Edge" },
            { CreateDefaultBrowserRegex("OPR"), "Opera" },
            { CreateDefaultBrowserRegex("Brave Chrome"), "Brave" },
            { CreateDefaultBrowserRegex("CriOS"), "Chrome" },
            { CreateDefaultBrowserRegex("MSIE"), "Internet Explorer" },
            { CreateDefaultBrowserRegex("Internet Explorer"), "Internet Explorer" },
            { CreateDefaultBrowserRegex("Trident.* rv"), "Internet Explorer" },
            
            // Other browsers
            { CreateDefaultBrowserRegex("Vivaldi"), "Vivaldi" },
            { CreateDefaultBrowserRegex("Flock"), "Flock" },
            { CreateDefaultBrowserRegex("Shiira"), "Shiira" },
            { CreateDefaultBrowserRegex("FxiOS"), "Firefox" },
            { CreateDefaultBrowserRegex("Chimera"), "Chimera" },
            { CreateDefaultBrowserRegex("Phoenix"), "Phoenix" },
            { CreateDefaultBrowserRegex("Firebird"), "Firebird" },
            { CreateDefaultBrowserRegex("Camino"), "Camino" },
            { CreateDefaultBrowserRegex("Netscape"), "Netscape" },
            { CreateDefaultBrowserRegex("OmniWeb"), "OmniWeb" },
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
            { CreateDefaultBrowserRegex("Smartstore"), "Smartstore" }
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
            { "sonyericsson", "Sony Ericsson" },
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
            { "nexus", "Nexus" },
            { "XOOM", "Motorola Xoom" },
            { "Transformer ", "Transformer" },
            { "kindle ", "Kindle" },
            { "silk ", "Kindle Fire" },
            { "playbook ", "Playbook" },
            { "puffin ", "Puffin" },
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
            // Common bots
            ( "googlebot", "Googlebot" ),
            ( "Applebot", "Applebot" ),
            ( "googleweblight", "Google Web Light" ),
            ( "YandexBot", "YandexBot"),
            ( "yandex-bot", "YandexBot"),
            ( "YandexImages", "YandexImages"),
            ( "mediapartners-google", "Mediapartners Google"),
            ( "apis-google", "APIs Google"),
            ( "Google Favicon", "Google Favicon"),
            ( "baiduspider", "Baiduspider"),
            ( "PetalBot", "PetalBot"),
            ( "bingbot", "BingBot"),
            ( "slurp", "Slurp"),
            ( "yahoo", "Yahoo"),
            ( "ask jeeves", "Ask Jeeves"),
            ( "AdsBot-Google-Mobile", "AdsBot Google Mobile"),
            ( "adsbot-google", "AdsBot Google"),
            ( "feedfetcher-google", "FeedFetcher-Google"),
            ( "facebookexternalhit", "Facebook"),
            ( "adscanner", "AdScanner"),
            ( "AhrefsBot", "Ahrefs"),

            // Common tools, readers & apps
            ( "python/", "Python"),
            ( "python-", "Python Requests"),
            ( "curl/", "cURL"),
            ( "urlwatch", "urlwatch"),
            ( "okhttp/", "OkHttp"),
            ( "HeadlessChrome", "Headless Chromium"),
            ( "Chrome-Lighthouse", "Chrome Lighthouse"),
            ( "Nessus", "Nessus"),
            ( "Pingdom", "Pingdom"),
            ( "axios/", "Axios"),
            ( "kube-prob/", "Kube Probe"),
            ( "Prometheus/", "Prometheus"),
            ( "GuzzleHttp/", "Guzzle Http"),
            ( "Feedly", "Feedly"),

            // Other bots
            ( "DuplexWeb-Google", "Google DuplexWeb"),
            ( "Storebot-Google", "Google Storebot"),
            ( "msnbot", "MSN Bot"),
            ( "Wappalyzer", "Wappalyzer"),
            ( "BW/1.1", "BuiltWith"),
            ( "PayPal IPN", "PayPal IPN"),
            ( "Amazonbot", "Amazonbot"),
            ( "AddThis.com", "AddThis.com"),
            ( "W3C", "W3C Validator/Checker"),
            ( "ShopAlike", "ShopAlike"),
            ( "ImageProxy", "ImageProxy"),
            ( "BingPreview", "Bing Preview"),
            ( "fastcrawler", "FastCrawler"),
            ( "infoseek", "InfoSeek Robot 1.0"),
            ( "lycos", "Lycos"),
            ( "Cloudflare", "Cloudflare"),
            ( "CRAZYWEBCRAWLER", "Crazy Webcrawler"),
            ( "google-read-aloud", "Google-Read-Aloud"),
            ( "curious george", "Curious George"),
            ( "ia_archiver", "Alexa Crawler"),
            ( "MJ12bot", "Majestic"),
            ( "Uptimebot", "Uptimebot"),
            ( "CheckMarkNetwork", "CheckMark"),
            ( "BLEXBot", "BLEXBot"),
            ( "DotBot", "OpenSite"),
            ( "Mail.RU_Bot", "Mail.ru"),
            ( "MegaIndex", "MegaIndex"),
            ( "SemrushBot", "SEMRush"),
            ( "SEOkicks", "SEOkicks"),
            ( "seoscanners.net", "SEO Scanners"),
            ( "Sistrix", "Sistrix" ),
            ( "check_http", "Check HTTP"),
            ( "NetNewsWire", "NetNewsWire RSS reader"),
            ( "iisbot", "IIS Bot"),
            ( "Yeti/", "Yeti bot"),
            ( "yacybot", "Yacy bot"),
            ( "crawler", "Generic crawler"),
            ( "-bot/", "Generic bot"),
        };

        public static bool IsTablet(string userAgent)
        {
            return TabletRegex.IsMatch(userAgent);
        }

        [GeneratedRegex("(ipad|tablet|(android(?!.*mobile))|(windows(?!.*phone)(.*touch))|kindle|playbook|silk|(puffin(?!.*(IP|AP|WP))))", RegexOptions.IgnoreCase | RegexOptions.Compiled, "de-DE")]
        private static partial Regex GeneratedTabletRegex();
    }
}