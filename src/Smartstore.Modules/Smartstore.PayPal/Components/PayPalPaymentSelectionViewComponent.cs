using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalPaymentSelectionViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalPaymentSelectionViewComponent(PayPalSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal buttons (PayPal, Sepa, PayLater) for payment selection page.
        /// </summary>
        /// <param name="isSelected">Defines whether the payment method is selected on page load.</param>
        public IViewComponentResult Invoke(bool isSelected)
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
                ButtonShape = _settings.ButtonShape
            };

            return View(model);
        }
    }
}