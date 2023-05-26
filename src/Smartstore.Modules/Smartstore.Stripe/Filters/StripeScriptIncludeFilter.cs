using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Identity;
using Smartstore.Core.Widgets;
using Smartstore.StripeElements.Services;
using Smartstore.StripeElements.Settings;

namespace Smartstore.StripeElements.Filters
{
    /// <summary>
    /// Renders the Stripe JS SDK script. Unfortunatelly it must be included in <head>-Tag.
    /// </summary>
    public class StripeScriptIncludeFilter : IAsyncActionFilter
    {
        private readonly StripeSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly StripeHelper _stripeHelper;
        private readonly ICookieConsentManager _cookieConsentManager;

        public StripeScriptIncludeFilter(ICookieConsentManager cookieConsentManager, StripeSettings settings, IWidgetProvider widgetProvider, StripeHelper stripeHelper)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _stripeHelper = stripeHelper;
            _cookieConsentManager = cookieConsentManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!_cookieConsentManager.IsCookieAllowed(CookieType.Required))
            {
                await next();
                return;
            }

            if (!await _stripeHelper.IsStripeElementsActive())
            {
                await next();
                return;
            }

            // If api key hasn't been configured yet, don't do anything.
            if (!_settings.SecrectApiKey.HasValue() || !_settings.PublicApiKey.HasValue())
            {
                await next();
                return;
            }
            
            _widgetProvider.RegisterHtml("scripts", new HtmlString("<script id=\"stripe-js\" src=\"/Modules/Smartstore.Stripe/smartstore.stripe.js\"></script>"));
            _widgetProvider.RegisterHtml("head", new HtmlString("<script id=\"stripe-js\" src=\"https://js.stripe.com/v3/\" async></script>"));
            
            await next();
        }
    }
}