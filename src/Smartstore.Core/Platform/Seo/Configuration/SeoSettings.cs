using System.Collections.Frozen;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo
{
    public enum CanonicalHostNameRule
    {
        /// <summary>
        /// Doesn't matter (as requested)
        /// </summary>
        NoRule,
        /// <summary>
        /// The www prefix is required (www.myshop.com is default host)
        /// </summary>
        RequireWww,
        /// <summary>
		/// The www prefix should be omitted (myshop.com is default host)
        /// </summary>
        OmitWww
    }

    /// <summary>
    /// Rule to apply when an incoming URL does not match
    /// the <see cref="SeoSettings.AppendTrailingSlashToUrls"/> setting.
    /// </summary>
    public enum TrailingSlashRule
    {
        /// <summary>
        /// Allow the other variant.
        /// </summary>
        Allow,
        /// <summary>
        /// Redirect to other variant (301).
        /// </summary>
        Redirect,
        /// <summary>
        /// Disallow the other variant and redirect to homepage.
        /// </summary>
        RedirectToHome,
        /// <summary>
        /// Disallow the other variant and return 404.
        /// </summary>
        Disallow
    }

    public class SeoSettings : ISettings
    {
        public static ISet<string> DefaultRobotDisallows { get; } = new HashSet<string>
        {
            "/admin/",
            "/bin/",
            "/exchange/",
            "/customer/",
            "/order/",
            "/install$",
            "/install/"
        };

        public static ISet<string> DefaultCharConversions { get; } = new HashSet<string>
        {
            "ä;ae",
            "ö;oe",
            "ü;ue",
            "ı;i",
            "Ä;Ae",
            "Ö;Oe",
            "Ü;Ue",
            "ß;ss"
        };

        private readonly object _lock = new();
        private FrozenDictionary<char, string> _charConversionMap = null;

        public SeoSettings()
        {
            ExtraRobotsDisallows = [];
            ExtraRobotsAllows = [];
            SeoNameCharConversion = string.Join(Environment.NewLine, DefaultCharConversions);
        }

        public string PageTitleSeparator { get; set; } = ". ";
        public PageTitleSeoAdjustment PageTitleSeoAdjustment { get; set; } = PageTitleSeoAdjustment.PagenameAfterStorename;

        /// <summary>
        /// Gets or sets the default meta title for the shop.
        /// </summary>
        [LocalizedProperty]
        public string MetaTitle { get; set; } = "Shop";

        [LocalizedProperty]
        public string MetaDescription { get; set; } = string.Empty;

        [LocalizedProperty]
        public string MetaKeywords { get; set; } = string.Empty;

        public string MetaRobotsContent { get; set; }

        public bool ConvertNonWesternChars { get; set; } = true;
        public bool AllowUnicodeCharsInUrls { get; set; }

        private string _seoNameCharConversion;
        public string SeoNameCharConversion
        {
            get
            {
                return _seoNameCharConversion;
            }
            set
            {
                if (value != _seoNameCharConversion)
                {
                    _charConversionMap = null;
                }

                _seoNameCharConversion = value;
            }
        }

        public bool CanonicalUrlsEnabled { get; set; }
        public CanonicalHostNameRule CanonicalHostNameRule { get; set; } = CanonicalHostNameRule.NoRule;

        //public bool LowercaseUrls { get; set; } = true;
        //public bool LowercaseQueryStrings { get; set; }
        public bool AppendTrailingSlashToUrls { get; set; } = true;
        public TrailingSlashRule TrailingSlashRule { get; set; } = TrailingSlashRule.Allow;

        public List<string> ExtraRobotsDisallows { get; set; }
        public List<string> ExtraRobotsAllows { get; set; }
        public string ExtraRobotsLines { get; set; }

        /// <summary>
        /// A value indicating whether to load all URL records and active slugs on application startup
        /// </summary>
        public bool LoadAllUrlAliasesOnStartup { get; set; } = true;

        public bool RedirectLegacyTopicUrls { get; set; }

        #region XML Sitemap

        public bool XmlSitemapEnabled { get; set; } = true;
        public bool XmlSitemapIncludesCategories { get; set; } = true;
        public bool XmlSitemapIncludesManufacturers { get; set; } = true;
        public bool XmlSitemapIncludesProducts { get; set; } = true;
        public bool XmlSitemapIncludesTopics { get; set; } = true;
        public bool XmlSitemapIncludesBlog { get; set; } = true;
        public bool XmlSitemapIncludesNews { get; set; } = true;
        public bool XmlSitemapIncludesForum { get; set; } = true;

        #endregion

        #region Computed

        /// <summary>
        /// Gets a cached char conversion map as specified by <see cref="SeoNameCharConversion"/>.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<char, string> GetCharConversionMap()
        {
            if (_charConversionMap == null)
            {
                lock (_lock)
                {
                    _charConversionMap ??= CreateCharConversionMap(SeoNameCharConversion).ToFrozenDictionary();
                }
            }

            return _charConversionMap;
        }

        public static Dictionary<char, string> CreateCharConversionMap(string charConversion)
        {
            var map = new Dictionary<char, string>();

            foreach (var conversion in charConversion.ReadLines(true, true))
            {
                if (conversion.SplitToPair(out var left, out var right, ";") && left.HasValue())
                {
                    map[left[0]] = right;
                }
            }

            return map;
        }

        #endregion
    }
}
