namespace Smartstore.Google.Analytics.Models
{
    [LocalizedDisplay("Plugins.Widgets.GoogleAnalytics.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("Admin.ContentManagement.Widgets.ChooseZone")]
        public string WidgetZone { get; set; }
        
        [LocalizedDisplay("*GoogleId")]
        public string GoogleId { get; set; }

        [LocalizedDisplay("*TrackingScript")]
        [UIHint("TextArea"), AdditionalMetadata("rows", 12)]
        public string TrackingScript { get; set; }

        [LocalizedDisplay("*EcommerceScript")]
        [UIHint("TextArea"), AdditionalMetadata("rows", 12)]
        public string EcommerceScript { get; set; }

        [LocalizedDisplay("*EcommerceDetailScript")]
        [UIHint("TextArea"), AdditionalMetadata("rows", 8)]
        public string EcommerceDetailScript { get; set; }
    }
}