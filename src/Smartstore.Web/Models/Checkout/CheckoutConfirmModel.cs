using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutConfirmModel : ModelBase
    {
        public List<string> Warnings { get; set; } = new();
        public bool TermsOfServiceEnabled { get; set; }
        public bool ShowEsdRevocationWaiverBox { get; set; }
        public bool BypassPaymentMethodInfo { get; set; }
        public bool? SubscribeToNewsletter { get; set; }
        public bool? AcceptThirdPartyEmailHandOver { get; set; }
        public string ThirdPartyEmailHandOverLabel { get; set; }
        public CheckoutNewsletterSubscription NewsletterSubscription { get; set; }
        public CheckoutThirdPartyEmailHandOver ThirdPartyEmailHandOver { get; set; }
    }
}
