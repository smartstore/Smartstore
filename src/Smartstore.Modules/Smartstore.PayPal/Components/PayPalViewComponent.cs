using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.PayPal.Models;
using Smartstore.PayPal.Settings;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalViewComponent : SmartViewComponent
    {
        private readonly ICommonServices _services;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly PayPalSettings _settings;
        
        public PayPalViewComponent(
            ICommonServices services,
            IShoppingCartService shoppingCartService,
            IOrderCalculationService orderCalculationService, 
            PayPalSettings settings)
        {
            _services = services;
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _settings = settings;
        }

        /// <summary>
        /// Renders PayPal button widget.
        /// </summary>
        /// <param name="isPaymentInfoInvoker">Defines whether the widget is invoked from payment method's GetPaymentInfoWidget.</param>
        /// <returns></returns>
        public async Task<IViewComponentResult> InvokeAsync(bool isPaymentInfoInvoker)
        {
            // If client id or secret haven't been configured yet, don't render button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }
            
            var controller = HttpContext.Request.RouteValues.GetControllerName().EmptyNull();
            var action = HttpContext.Request.RouteValues.GetActionName().EmptyNull();
            var isPaymentSelectionPage = controller == "Checkout" && action == "PaymentMethod";

            if (isPaymentSelectionPage && isPaymentInfoInvoker)
            {
                return Empty();
            }

            var cart = await _shoppingCartService.GetCartAsync(Services.WorkContext.CurrentCustomer, ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id);
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);
            var model = new PublicPaymentMethodModel
            {
                Intent = _settings.Intent,
                Amount = cartSubTotal.SubtotalWithDiscount.Amount,
                IsPaymentSelection = isPaymentSelectionPage
            };

            var currency = _services.WorkContext.WorkingCurrency.CurrencyCode;
            var isConfirmationPage = controller == "Checkout" && action == "Confirm";

            var scriptUrl = $"https://www.paypal.com/sdk/js" +
                $"?client-id={_settings.ClientId}" +
                $"&currency={currency}" +
                // Ensures no breaking changes will be applied in SDK.
                $"&integration-date=2021-12-14" +
                // Depends on the button location. Commit on confirmation page.
                $"&commit={isConfirmationPage.ToString().ToLower()}";

            if (_settings.DisabledFundings.HasValue())
            {
                scriptUrl += $"&disable-funding={GetFundingOptions<DisableFundingOptions>(_settings.DisabledFundings)}";
            }

            if (_settings.EnabledFundings.HasValue())
            {
                scriptUrl += $"&enable-funding={GetFundingOptions<EnableFundingOptions>(_settings.EnabledFundings)}";
            }

            scriptUrl += $"&intent={_settings.Intent}";
            scriptUrl += $"&locale={_services.WorkContext.WorkingLanguage.LanguageCulture.Replace("-", "_")}";

            model.ScriptUrl = scriptUrl;

            return View(model);
        }

        private static string GetFundingOptions<TEnum>(string fundings) where TEnum : struct
        {
            var result = string.Empty;
            TEnum resultInputType = default;

            var arr = fundings.SplitSafe(',')
                .Select(x =>
                {
                    Enum.TryParse(x, true, out resultInputType);
                    return resultInputType.ToString();
                })
                .ToArray();

            if (arr.Length > 0)
            {
                result = string.Join(',', arr ?? Array.Empty<string>());
            }

            return result;
        }
    }
}