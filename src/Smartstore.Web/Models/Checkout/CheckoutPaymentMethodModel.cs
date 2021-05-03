using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Web.Modelling;
using System.Collections.Generic;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutPaymentMethodModel : ModelBase
    {
        public List<PaymentMethodModel> PaymentMethods { get; set; } = new();

        public bool SkippedSelectShipping { get; set; }

        public partial class PaymentMethodModel : ModelBase
        {
            public string PaymentMethodSystemName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public LocalizedValue<string> FullDescription { get; set; }
            public string BrandUrl { get; set; }
            public Money Fee { get; set; }
            public bool Selected { get; set; }
            public WidgetInvoker InfoWidget { get; set; }
            public bool RequiresInteraction { get; set; }
        }
    }
}