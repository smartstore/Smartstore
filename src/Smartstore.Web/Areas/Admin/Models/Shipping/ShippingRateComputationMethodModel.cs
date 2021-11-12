using Smartstore.Admin.Models.Modularity;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Shipping
{
    public class ShippingRateComputationMethodModel : ProviderModel, IActivatable
    {
        [LocalizedDisplay("Common.IsActive")]
        public bool IsActive { get; set; }
    }
}
