using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo
{
    public class SeoSettings : ISettings
    {
        public static ISet<string> DefaultRobotDisallows { get; } = new HashSet<string>
        {
            "/bin/",
            "/Exchange/",
            "/Country/GetStatesByCountryId",
            "/Install$",
            "/Product/SetReviewHelpfulness"
        };

        public static ISet<string> DefaultRobotLocalizableDisallows { get; } = new HashSet<string>
        {
            "/Cart$",
            "/Checkout",
            "/Product/ClearCompareList",
            "/CompareProducts",
            "/Customer/Avatar",
            "/Customer/Activation",
            "/Customer/Addresses",
            "/Customer/BackInStockSubscriptions",
            "/Customer/ChangePassword",
            "/Customer/CheckUsernameAvailability",
            "/Customer/DownloadableProducts",
            "/Customer/ForumSubscriptions",
            "/Customer/DeleteForumSubscriptions",
            "/Customer/Info",
            "/Customer/Orders",
            "/Customer/ReturnRequests",
            "/Customer/RewardPoints",
            "/Newsletter/SubscriptionActivation",
            "/Order$",
            "/PasswordRecovery",
            "/ReturnRequest",
            "/Newsletter/Subscribe",
            "/Topic/Authenticate",
            "/Wishlist",
            "/Product/AskQuestion",
            "/Product/EmailAFriend",
            "/Cookiemanager",
			//"/Search",
			"/Config$",
            "/Settings$",
            "/Login$",
            "/Login?*",
            "/Register$",
            "/Register?*"
        };

        public static ISet<string> DefaultCharConversions { get; } = new HashSet<string>
        {
            "ä;ae",
            "ö;oe",
            "ü;ue",
            "Ä;Ae",
            "Ö;Oe",
            "Ü;Ue",
            "ß;ss"
        };

        public static string[] DefaultReservedUrlRecordSlugs { get; } = new[]
        {
            "admin",
            "install",
            "recentlyviewedproducts",
            "newproducts",
            "compareproducts",
            "clearcomparelist",
            "setproductreviewhelpfulness",
            "login",
            "register",
            "logout",
            "cart",
            "wishlist",
            "emailwishlist",
            "checkout",
            "contactus",
            "passwordrecovery",
            "subscribenewsletter",
            "blog",
            "boards",
            "forum",
            "inboxupdate",
            "sentupdate",
            "news",
            "sitemap",
            "sitemapseo",
            "search",
            "config",
            "api",
            "odata"
        };

        private readonly object _lock = new();
        private Dictionary<char, string> _charConversionMap = null;

        public SeoSettings()
        {
            ExtraRobotsDisallows = new List<string> { "/blog/tag/", "/blog/month/", "/producttags/" };
            ExtraRobotsAllows = new List<string>();
            ReservedUrlRecordSlugs = new HashSet<string>(DefaultReservedUrlRecordSlugs, StringComparer.OrdinalIgnoreCase);
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

        /// <summary>
        /// Slugs (sename) reserved for some other needs
        /// </summary>
        public HashSet<string> ReservedUrlRecordSlugs { get; set; }

        public List<string> ExtraRobotsDisallows { get; set; }
        public List<string> ExtraRobotsAllows { get; set; }

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
                    if (_charConversionMap == null)
                    {
                        _charConversionMap = CreateCharConversionMap(SeoNameCharConversion);
                    }
                }
            }

            return _charConversionMap;
        }

        public static Dictionary<char, string> CreateCharConversionMap(string charConversion)
        {
            var map = new Dictionary<char, string>();

            foreach (var conversion in charConversion.SplitSafe(Environment.NewLine))
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
