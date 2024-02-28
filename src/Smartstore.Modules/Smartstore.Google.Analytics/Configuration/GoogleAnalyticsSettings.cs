using Smartstore.Core.Configuration;

namespace Smartstore.Google.Analytics.Settings
{
    public class GoogleAnalyticsSettings : ISettings
    {
        public string GoogleId { get; set; } = "UA-0000000-0";
        public bool RenderWithUserConsentOnly { get; set; }

        /// <summary>
        /// Specifies whether to display cookie infos for ads. 
        /// These infos are only relevant if the shop uses Google Ads.
        /// If only analytics is used, this setting can remain set to false.
        /// </summary>
        public bool DisplayCookieInfosForAds { get; set; }

        public string TrackingScript { get; set; }
        public string EcommerceScript { get; set; }
        public string EcommerceDetailScript { get; set; }
        public bool RenderCatalogScripts { get; set; } = false;
        public bool RenderCheckoutScripts { get; set; } = false;
        public bool MinifyScripts { get; set; } = true;
    }
}