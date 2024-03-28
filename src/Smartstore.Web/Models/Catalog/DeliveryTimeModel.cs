using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public partial class DeliveryTimeModel : EntityModelBase
    {
        public bool ShowDeliveryTime { get; set; }
        public bool DisplayDeliveryTimeAccordingToStock { get; set; }

        public LocalizedValue<string> DeliveryTimeName { get; set; }
        public string DeliveryTimeHexValue { get; set; }

        public string DeliveryTimeDate { get; set; }
        public string StatusLabel { get; set; }
        public string StockAvailability { get; set; }
    }
}
