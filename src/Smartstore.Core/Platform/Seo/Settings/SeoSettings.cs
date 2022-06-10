using Smartstore.Core.Configuration;

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
                "/Product/SetReviewHelpfulness",
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

        public SeoSettings()
        {
            ExtraRobotsDisallows = new List<string> { "/blog/tag/", "/blog/month/", "/producttags/" };
            ExtraRobotsAllows = new List<string>();

            ReservedUrlRecordSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

            SeoNameCharConversion = string.Join(Environment.NewLine, new List<string>
            {
                "ä;ae",
                "ö;oe",
                "ü;ue",
                "Ä;Ae",
                "Ö;Oe",
                "Ü;Ue",
                "ß;ss"
            });
        }

        public string PageTitleSeparator { get; set; } = ". ";
        public PageTitleSeoAdjustment PageTitleSeoAdjustment { get; set; } = PageTitleSeoAdjustment.PagenameAfterStorename;

        /// <summary>
        /// Gets or sets the default meta title for the shop.
        /// </summary>
        public string MetaTitle { get; set; } = "Shop";
        public string MetaDescription { get; set; } = string.Empty;
        public string MetaKeywords { get; set; } = string.Empty;

        public string MetaRobotsContent { get; set; }

        public bool ConvertNonWesternChars { get; set; }
        public bool AllowUnicodeCharsInUrls { get; set; }
        public string SeoNameCharConversion { get; set; }

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
    }
}
