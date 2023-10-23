using Smartstore.Shipping.Settings;

namespace Smartstore.Shipping.Models
{
    [LocalizedDisplay("Plugins.Shipping.ByTotal.Fields.")]
    public class ByTotalListModel : ModelBase
    {
        [LocalizedDisplay("*LimitMethodsToCreated")]
        public bool LimitMethodsToCreated { get; set; }

        [LocalizedDisplay("*SmallQuantityThreshold")]
        public decimal SmallQuantityThreshold { get; set; }

        [LocalizedDisplay("*SmallQuantitySurcharge")]
        public decimal SmallQuantitySurcharge { get; set; }

        [LocalizedDisplay("*CalculateTotalIncludingTax")]
        public bool CalculateTotalIncludingTax { get; set; }
    }

    public partial class ShippingByTotalListValidator : SettingModelValidator<ByTotalListModel, ShippingByTotalSettings>
    {
        public ShippingByTotalListValidator()
        {
            RuleFor(x => x.SmallQuantityThreshold).GreaterThanOrEqualTo(0);
        }
    }
}
