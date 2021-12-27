using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Adds JavaScript click event handlers to enable updating of payment and shipping in checkout.
    /// Also adds the script for the AmazonPay confirmation flow.
    /// </summary>
    public class ConfirmOrderViewComponent : SmartViewComponent
    {
        private readonly AmazonPaySettings _settings;

        public ConfirmOrderViewComponent(AmazonPaySettings settings)
        {
            _settings = settings;
        }

        public IViewComponentResult Invoke(AmazonPayCheckoutState state)
        {
            Guard.NotNull(state, nameof(state));

            return View(new ConfirmOrderModel(_settings, state));
        }
    }
}
