using Smartstore.Web.Models.Common;

namespace Smartstore.AmazonPay.Models
{
    public class AmazonPayViewModel : ModelBase
    {
        public string SellerId { get; set; }
        public string ClientId { get; set; }

        /// <summary>
        /// Amazon widget script URL
        /// </summary>
        public string WidgetUrl { get; set; }
        public string ButtonHandlerUrl { get; set; }

        public bool IsShippable { get; set; } = true;
        public bool IsRecurring { get; set; }

        public string LanguageCode { get; set; }
        public AmazonPayRequestType Type { get; set; }
        public AmazonPayResultType Result { get; set; } = AmazonPayResultType.PluginView;

        public string RedirectAction { get; set; } = "Cart";
        public string RedirectController { get; set; } = "ShoppingCart";

        public string OrderReferenceId { get; set; }
        public string AddressConsentToken { get; set; }
        public string Warning { get; set; }
        public bool Logout { get; set; }

        public string ButtonType { get; set; }
        public string ButtonColor { get; set; }
        public string ButtonSize { get; set; }

        public string ShippingMethod { get; set; }
        public AddressModel BillingAddress { get; set; } = new();

        // Confirmation flow.
        public bool IsConfirmed { get; set; }
        public string FormData { get; set; }
        public bool SubmitForm { get; set; }
    }
}
