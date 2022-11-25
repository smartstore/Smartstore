using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Widgets;
using Smartstore.StripeElements.Providers;
using Smartstore.StripeElements.Settings;

namespace Smartstore.StripeElements.Filters
{
    /// <summary>
    /// Renders the Stripe JS SDK script. Unfortunatelly it must be included in <head>-Tag.
    /// </summary>
    public class ScriptIncludeFilter : IAsyncActionFilter
    {
        private readonly StripeSettings _settings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;
        
        public ScriptIncludeFilter(
            StripeSettings settings, 
            IWidgetProvider widgetProvider,
            ICommonServices services,
            IPaymentService paymentService)
        {
            _settings = settings;
            _widgetProvider = widgetProvider;
            _services = services;
            _paymentService = paymentService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!await IsStripeElementsActive())
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

        private Task<bool> IsStripeElementsActive()
            => _paymentService.IsPaymentMethodActiveAsync(StripeElementsProvider.SystemName, null, _services.StoreContext.CurrentStore.Id);
    }
}