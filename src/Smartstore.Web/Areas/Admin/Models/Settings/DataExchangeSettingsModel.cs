using Smartstore.Core.DataExchange;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.DataExchange.")]
    public partial class DataExchangeSettingsModel
    {
        [LocalizedDisplay("*MaxFileNameLength")]
        public int MaxFileNameLength { get; set; }

        [LocalizedDisplay("*ImageImportFolder")]
        public string ImageImportFolder { get; set; }

        [LocalizedDisplay("*ImageDownloadTimeout")]
        public int ImageDownloadTimeout { get; set; }

        [LocalizedDisplay("*ImportCompletionEmail")]
        public DataExchangeCompletionEmail ImportCompletionEmail { get; set; }
    }
}
