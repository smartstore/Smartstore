using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Logging;
using Smartstore.Core.Widgets;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Filters
{
    public class CheckoutFilter : IAsyncActionFilter
    {
        private static readonly string[] _skipActions = new[] 
        {
            nameof(CheckoutController.BillingAddress),
            nameof(CheckoutController.ShippingAddress),
            nameof(CheckoutController.PaymentMethod),
        };

        private readonly ICommonServices _services;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly OrderSettings _orderSettings;

        public CheckoutFilter(
            ICommonServices services,
            Lazy<IPaymentService> paymentService,
            Lazy<IUrlHelper> urlHelper,
            Lazy<IWidgetProvider> widgetProvider,
            ICheckoutStateAccessor checkoutStateAccessor,
            OrderSettings orderSettings)
        {
            _services = services;
            _paymentService = paymentService;
            _urlHelper = urlHelper;
            _widgetProvider = widgetProvider;
            _checkoutStateAccessor = checkoutStateAccessor;
            _orderSettings = orderSettings;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_services.WorkContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod.EqualsNoCase(AmazonPayProvider.SystemName))
            {
                if (await _paymentService.Value.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, null, _services.StoreContext.CurrentStore.Id))
                {
                    var action = context.RouteData.Values.GetActionName();

                    if (_skipActions.Contains(action, StringComparer.OrdinalIgnoreCase))
                    {
                        context.Result = new RedirectResult(_urlHelper.Value.Action(nameof(CheckoutController.Confirm), "Checkout"));
                    }
                    else if (action.EqualsNoCase(nameof(CheckoutController.Confirm)))
                    {
                        if (_checkoutStateAccessor.CheckoutState?.CustomProperties?.Get(AmazonPayProvider.CheckoutStateKey) is AmazonPayCheckoutState state
                            && state.CheckoutSessionId.HasValue())
                        {
                            _widgetProvider.Value.RegisterWidget("end",
                                new PartialViewWidgetInvoker("_ConfirmOrder", state, "Smartstore.AmazonPay"));
                        }
                    }
                    else if (action.EqualsNoCase(nameof(CheckoutController.Completed)))
                    {
                        var note = context.HttpContext.Session.GetString("AmazonPayCheckoutCompletedNote");
                        if (note.HasValue())
                        {
                            if (_orderSettings.DisableOrderCompletedPage)
                            {
                                _services.Notifier.Information(note);
                            }
                            else
                            {
                                _widgetProvider.Value.RegisterHtml("checkout_completed_top", new HtmlString(note));
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}
