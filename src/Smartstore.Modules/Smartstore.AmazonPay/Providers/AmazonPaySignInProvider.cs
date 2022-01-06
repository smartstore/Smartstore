using Smartstore.AmazonPay.Components;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;

namespace Smartstore.AmazonPay.Providers
{
    [SystemName("Authentications.AmazonPay")]
    [FriendlyName("Amazon sign-in")]
    [Order(-1)]
    public class AmazonPaySignInProvider : IExternalAuthenticationMethod
    {
        public static string SystemName => "Authentications.AmazonPay";

        public WidgetInvoker GetDisplayWidget(int storeId)
            => new ComponentWidgetInvoker(typeof(SignInButtonViewComponent));
    }
}
