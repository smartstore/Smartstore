using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Identity;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin
{
    public static class UrlHelperExtensions
    {
        public static string BackToProviderList(this IUrlHelper url, ProviderMetadata metadata)
        {
            Guard.NotNull(metadata, nameof(metadata));

            if (metadata.ProviderType == typeof(IPaymentMethod))
            {
                return url.Action("Providers", "Payment");
            }
            else if (metadata.ProviderType == typeof(ITaxProvider))
            {
                return url.Action("Providers", "Tax");
            }
            else if (metadata.ProviderType == typeof(IShippingRateComputationMethod))
            {
                return url.Action("Providers", "Shipping");
            }
            else if (metadata.ProviderType == typeof(IActivatableWidget))
            {
                return url.Action("Providers", "Widget");
            }
            else if (metadata.ProviderType == typeof(IExternalAuthenticationMethod))
            {
                return url.Action("Providers", "ExternalAuthentication");
            }

            return null;
        }
    }
}
