namespace Smartstore.Google.Analytics.Models
{
    [LocalizedDisplay("Plugins.Widgets.GoogleAnalytics.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*GoogleId")]
        public string GoogleId { get; set; }

        [LocalizedDisplay("*RenderWithUserConsentOnly")]
        public bool RenderWithUserConsentOnly { get; set; }

        [LocalizedDisplay("*DisplayCookieInfosForAds")]
        public bool DisplayCookieInfosForAds { get; set; }

        [LocalizedDisplay("*TrackingScript")]
        [UIHint("TextArea"), AdditionalMetadata("rows", 12)]
        public string TrackingScript { get; set; }

        [LocalizedDisplay("*EcommerceScript")]
        [UIHint("TextArea"), AdditionalMetadata("rows", 12)]
        public string EcommerceScript { get; set; }

        [LocalizedDisplay("*EcommerceDetailScript")]
        [UIHint("TextArea"), AdditionalMetadata("rows", 8)]
        public string EcommerceDetailScript { get; set; }

        [LocalizedDisplay("*RenderCatalogScripts")]
        public bool RenderCatalogScripts { get; set; }

        [LocalizedDisplay("*RenderCheckoutScripts")]
        public bool RenderCheckoutScripts { get; set; }

        [LocalizedDisplay("*MinifyScripts")]
        public bool MinifyScripts { get; set; }

        public bool ScriptUpdateRecommended { get; set; }
    }
}