namespace Smartstore.AmazonPay.Models
{
    public class AmazonPayViewModel : ModelBase
    {
        public AmazonPayViewModel()
        {
        }

        public AmazonPayViewModel(AmazonPaySettings settings)
        {
            Guard.NotNull(settings, nameof(settings));

            PublicKeyId = settings.PublicKeyId;
            PrivateKey = settings.PrivateKey;
            SellerId = settings.SellerId;
            ClientId = settings.ClientId;
            // AmazonPay review: The setting for payment button type has been removed.
            ButtonType = "PwA";
            ButtonColor = settings.PayButtonColor;
            ButtonSize = settings.PayButtonSize;

            switch (settings.Marketplace.EmptyNull().ToLower())
            {
                case "us":
                    CheckoutScriptUrl = "https://static-na.payments-amazon.com/checkout.js";
                    break;
                case "jp":
                    CheckoutScriptUrl = "https://static-fe.payments-amazon.com/checkout.js";
                    break;
                default:
                    CheckoutScriptUrl = "https://static-eu.payments-amazon.com/checkout.js";
                    break;
            }
        }

        public string PublicKeyId { get; set; }
        public string PrivateKey { get; set; }

        public string SellerId { get; set; }
        public string ClientId { get; set; }

        public string CheckoutScriptUrl { get; set; }
        public string ButtonHandlerUrl { get; set; }

        public bool IsShippable { get; set; } = true;
        public bool IsRecurring { get; set; }

        public string LanguageCode { get; set; }
        //public AmazonPayRequestType Type { get; set; }
        //public AmazonPayResultType Result { get; set; } = AmazonPayResultType.PluginView;

        //public string RedirectAction { get; set; } = "Cart";
        //public string RedirectController { get; set; } = "ShoppingCart";

        //public string OrderReferenceId { get; set; }
        //public string AddressConsentToken { get; set; }
        //public string Warning { get; set; }
        //public bool Logout { get; set; }

        public string ButtonType { get; set; }
        public string ButtonColor { get; set; }
        public string ButtonSize { get; set; }

        //public string ShippingMethod { get; set; }
        //public AddressModel BillingAddress { get; set; } = new();

        // Confirmation flow.
        //public bool IsConfirmed { get; set; }
        //public string FormData { get; set; }
        //public bool SubmitForm { get; set; }
    }
}
