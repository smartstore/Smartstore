using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponentBase : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalViewComponentBase(PayPalSettings settings)
        {
            _settings = settings;
        }

        // TODO: (mh) Try to use base view component.
        /// <summary>
        /// Renders PayPal buttons widget.
        /// </summary>
        public IViewComponentResult Invoke()
        {
            // If client id or secret haven't been configured yet, don't render buttons.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var routeIdent = Request.RouteValues.GenerateRouteIdentifier();
            
            var model = new PublicPaymentMethodModel
            {
                ButtonColor = _settings.ButtonColor,
                ButtonShape = _settings.ButtonShape
            };

            return View(model);
        }
    }
}