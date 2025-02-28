using Microsoft.AspNetCore.Mvc;
using Smartstore.PayPal.Services;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalPaymentSelectionViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;
        private readonly PayPalHelper _payPalHelper;

        public PayPalPaymentSelectionViewComponent(PayPalSettings settings, PayPalHelper payPalHelper)
        {
            _settings = settings;
            _payPalHelper = payPalHelper;
        }

        /// <summary>
        /// Renders PayPal buttons (PayPal, Sepa, PayLater) for payment selection page.
        /// </summary>
        /// <param name="funding">Defines the funding source of the payment method selected on page load.</param>
        /// <param name="isSelected">Defines whether the payment method is selected on page load.</param>
        public async Task<IViewComponentResult> InvokeAsync(string funding, bool isSelected)
        {
            // If client id or secret haven't been configured yet, don't render buttons.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var model = new PublicPaymentMethodModel
            {
                IsPaymentSelection = true,
                IsSelectedMethod = isSelected,
                ButtonColor = _settings.ButtonColor,
                ButtonShape = _settings.ButtonShape,
                Funding = funding,
                IsSandbox = _settings.UseSandbox,
                IsGooglePayActive = await _payPalHelper.IsProviderActiveAsync(PayPalConstants.GooglePay)
            };

            return View(model);
        }
    }
}