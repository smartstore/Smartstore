using Microsoft.AspNetCore.Mvc;
using Smartstore.PayPal.Client;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponent : SmartViewComponent
    {
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;

        public PayPalViewComponent(PayPalHttpClient client, PayPalSettings settings)
        {
            _client = client;
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal buttons widget.
        /// </summary>
        /// <param name="isPaymentInfoInvoker">Defines whether the widget is invoked from payment method's GetPaymentInfoWidget.</param>
        /// <param name="isSelected">Defines whether the payment method is selected on page load.</param>
        public async Task<IViewComponentResult> InvokeAsync(bool isPaymentInfoInvoker, bool isSelected)
        {
            // If client id or secret haven't been configured yet, don't render buttons.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var routeIdent = Request.RouteValues.GenerateRouteIdentifier();
            var isPaymentSelectionPage = routeIdent == "Checkout.PaymentMethod";

            if (isPaymentSelectionPage && isPaymentInfoInvoker)
            {
                return Empty();
            }

            // Get displayable options from settings depending on location (OffCanvasCart or Cart).
            var isCartPage = routeIdent == "ShoppingCart.Cart";
            var fundingEnumIds = isCartPage ? 
                _settings.FundingsCart.ToIntArray() :
                _settings.FundingsOffCanvasCart.ToIntArray();

            var fundings = string.Empty;
            foreach (var fundingId in fundingEnumIds)
            {
                fundings += ((EnableFundingOptions)fundingId).ToStringInvariant() + ',';
            }

            var model = new PublicPaymentMethodModel
            {
                IsPaymentSelection = isPaymentSelectionPage,
                ButtonColor = _settings.ButtonColor,
                ButtonShape = _settings.ButtonShape,
                IsSelectedMethod = isSelected,
                Fundings = fundings,
                OrderJson = JsonConvert.SerializeObject(await _client.GetOrderForStandardProviderAsync(!isPaymentSelectionPage))
            };

            return View(model);
        }
    }
}