using Smartstore.Core.Checkout.Cart;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutConfirmModel : CheckoutModelBase
    {
        public bool ShowEsdRevocationWaiverBox { get; set; }
        public bool? SubscribeToNewsletter { get; set; }
        public bool? AcceptThirdPartyEmailHandOver { get; set; }
        public string ThirdPartyEmailHandOverLabel { get; set; }
        public string TermsOfService { get; set; }
        public CheckoutNewsletterSubscription NewsletterSubscription { get; set; }
        public CheckoutThirdPartyEmailHandOver ThirdPartyEmailHandOver { get; set; }
        public ShoppingCartModel ShoppingCart { get; set; }
    }
}
