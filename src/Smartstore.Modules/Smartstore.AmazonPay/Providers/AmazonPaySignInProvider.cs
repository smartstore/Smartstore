using Smartstore.AmazonPay.Components;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;

namespace Smartstore.AmazonPay.Providers
{
    [SystemName("Smartstore.AmazonPay")]
    [FriendlyName("Amazon sign-in")]
    [Order(-1)]
    public class AmazonPaySignInProvider : IExternalAuthenticationMethod
    {
        // Keep old provider name for compatibility (see ExternalAuthenticationRecord.ProviderSystemName).
        public static string SystemName => "Smartstore.AmazonPay";

        public Widget GetDisplayWidget(int storeId)
            => new ComponentWidget(typeof(SignInButtonViewComponent));
    }
}
