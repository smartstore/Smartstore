using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutPaymentMethodModel : CheckoutModelBase
    {
        public List<PaymentMethodModel> PaymentMethods { get; set; } = [];

        public bool DisplayPaymentMethodIcons { get; set; }

        public partial class PaymentMethodModel : ModelBase
        {
            public string PaymentMethodSystemName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public LocalizedValue<string> FullDescription { get; set; }
            public string BrandUrl { get; set; }
            public string IconUrl { get; set; }
            public Money Fee { get; set; }
            public bool Selected { get; set; }
            public Widget InfoWidget { get; set; }
            public bool RequiresInteraction { get; set; }
        }
    }
}