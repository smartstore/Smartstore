using System;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Settings;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal.Filters
{
    public class ScriptIncludeFilter : IResultFilter
    {
        private readonly ICommonServices _services;
        private readonly PayPalSettings _settings;
        private readonly Lazy<IWidgetProvider> _widgetProvider;

        public ScriptIncludeFilter(ICommonServices services, PayPalSettings settings, Lazy<IWidgetProvider> widgetProvider)
        {
            _services = services;
            _settings = settings;
            _widgetProvider = widgetProvider;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // If client id or secret haven't been configured yet, don't render script.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
                return;

            var controller = filterContext.RouteData.Values.GetControllerName().EmptyNull();
            var action = filterContext.RouteData.Values.GetActionName().EmptyNull();
            var isConfirmationPage = controller == "Checkout" && action == "Confirm";
            var isPaymentSelectionPage = controller == "Checkout" && action == "PaymentMethod";
            var isCartPage = controller == "ShoppingCart" && action == "Cart";

            // TODO: (mh) (core) Consider rendering script via dynamic script include, as it's needed globaly only if minibasket will be show (via AJAX).

            // Render on cart, payment selection, order confirmation or everywhere if mini cart setting is turned on.
            if ((!isConfirmationPage || !isPaymentSelectionPage || !isCartPage) && !_settings.ShowButtonInMiniShoppingCart)
                return;

            // Should only run on a full view rendering result or HTML ContentResult.
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                var currency = _services.WorkContext.WorkingCurrency.CurrencyCode;

                var html = $"<script src='https://www.paypal.com/sdk/js" +
                    $"?client-id={_settings.ClientId}" +
                    $"&currency={currency}" +
                    $"&integration-date=2021-12-14" +                                           // Ensures no breaking changes will be applied in SDK.
                    $"&commit={isConfirmationPage.ToString().ToLower()}";                       // Depends on the button location. Commit on confirmation page.

                if (_settings.DisabledFundings.HasValue())
                {
                    html += $"&disable-funding={GetFundingOptions<DisableFundingOptions>(_settings.DisabledFundings)}";
                }

                if (_settings.EnabledFundings.HasValue())
                {
                    html += $"&enable-funding={GetFundingOptions<EnableFundingOptions>(_settings.EnabledFundings)}";
                }

                html += $"&intent={_settings.Intent}";
                html += "'></script>";

                _widgetProvider.Value.RegisterHtml("head_scripts", new HtmlString(html));
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        private static string GetFundingOptions<TEnum>(string fundings) where TEnum : struct
        {
            var concated = string.Empty;
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
                concated = string.Join(',', arr ?? Array.Empty<string>());
            }

            return concated;
        }
    }
}

