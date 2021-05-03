using Smartstore.Core.Checkout.Cart;
using Smartstore.Web.Modelling;
using System.Collections.Generic;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutConfirmModel : ModelBase
    {
        public List<string> Warnings { get; set; } = new();
        public bool TermsOfServiceEnabled { get; set; }
        public bool ShowEsdRevocationWaiverBox { get; set; }
        public bool BypassPaymentMethodInfo { get; set; }
        public bool? SubscribeToNewsLetter { get; set; }
        public bool? AcceptThirdPartyEmailHandOver { get; set; }
        public string ThirdPartyEmailHandOverLabel { get; set; }
        public CheckoutNewsLetterSubscription NewsLetterSubscription { get; set; }
        public CheckoutThirdPartyEmailHandOver ThirdPartyEmailHandOver { get; set; }
    }
}
