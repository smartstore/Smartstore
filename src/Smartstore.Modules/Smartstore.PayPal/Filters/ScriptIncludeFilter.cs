using System;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.PayPal.Settings;

namespace Smartstore.PayPal.Filters
{
    public class ScriptIncludeFilter : IResultFilter
    {
        private readonly ICommonServices _services;
        private readonly PayPalSettings _settings;
        private readonly IWidgetProvider _widgetProvider;

        public ScriptIncludeFilter(ICommonServices services, PayPalSettings settings, IWidgetProvider widgetProvider)
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

            // TODO: (mh) (core) Consider rendering script via dynamic script include, as it's needed globaly only if minibasket will be shown (via AJAX).

            // Should only run on a full view rendering result or HTML ContentResult.
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                var currency = _services.WorkContext.WorkingCurrency.CurrencyCode;

                var html = $"<script src='https://www.paypal.com/sdk/js" +
                    $"?client-id={_settings.ClientId}" +
                    $"&currency={currency}" +
                    // Ensures no breaking changes will be applied in SDK.
                    $"&integration-date=2021-12-14" +
                    // Depends on the button location. Commit on confirmation page.
                    $"&commit={isConfirmationPage.ToString().ToLower()}";

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

                _widgetProvider.RegisterHtml("head_scripts", new HtmlString(html));
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
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

