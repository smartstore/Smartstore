namespace Smartstore.Google.MerchantCenter.Models
{
    [LocalizedDisplay("Plugins.Feed.Froogle.")]
    public class ConfigurationModel
    {
        [LocalizedDisplay("*SearchProductName")]
        public string SearchProductName { get; set; }

        [LocalizedDisplay("*SearchProductSku")]
        public string SearchProductSku { get; set; }

        [LocalizedDisplay("*SearchIsTouched")]
        public bool? SearchIsTouched { get; set; }
    }
}
