using Smartstore.Admin.Models.Modularity;

namespace Smartstore.Admin.Models.Shipping
{
    public class ShippingRateComputationMethodModel : ProviderModel, IActivatable
    {
        [LocalizedDisplay("Common.IsActive")]
        public bool IsActive { get; set; }
    }
}
