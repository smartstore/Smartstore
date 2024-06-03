using Smartstore.Core.Checkout.Cart;
using Smartstore.Web.Models.Cart;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutConfirmModel : CheckoutModelBase
    {
        public bool ShowSecondBuyButtonBelowCart { get; set; } = true;
        public bool ShowEsdRevocationWaiverBox { get; set; }
        public bool? SubscribeToNewsletter { get; set; }
        public bool? AcceptThirdPartyEmailHandOver { get; set; }
        public string ThirdPartyEmailHandOverLabel { get; set; }
        public string TermsOfService { get; set; }
        public CheckoutNewsletterSubscription NewsletterSubscription { get; set; }
        public CheckoutThirdPartyEmailHandOver ThirdPartyEmailHandOver { get; set; }
        public ShoppingCartModel ShoppingCart { get; set; }
        public OrderReviewDataModel OrderReviewData { get; set; }

        public partial class OrderReviewDataModel : ModelBase
        {
            public bool IsBillingAddressRequired { get; set; }
            public AddressModel BillingAddress { get; set; }

            public bool IsShippable { get; set; }
            public AddressModel ShippingAddress { get; set; }
            public string ShippingMethod { get; set; }
            public bool DisplayShippingMethodChangeOption { get; set; }

            public bool IsPaymentRequired { get; set; }
            public string PaymentMethod { get; set; }
            public string PaymentSummary { get; set; }
            public bool IsPaymentSelectionSkipped { get; set; }
            public bool DisplayPaymentMethodChangeOption { get; set; }
        }
    }
}
