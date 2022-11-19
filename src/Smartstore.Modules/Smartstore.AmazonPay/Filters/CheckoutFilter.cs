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

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!await IsAmazonPayActive())
            {
                await next();
                return;
            }

            var action = context.RouteData.Values.GetActionName();

            if (action.EqualsNoCase(nameof(CheckoutController.Completed)))
            {
                var responseStatus = context.HttpContext.Session.GetString("AmazonPayResponseStatus");
                if (responseStatus.HasValue())
                {
                    // 202 (Accepted): authorization is pending.
                    var completedNote = responseStatus == "202"
                        ? T("Plugins.Payments.AmazonPay.AsyncPaymentAuthorizationNote").Value
                        : string.Empty;

                    if (!_orderSettings.DisableOrderCompletedPage)
                    {
                        _widgetProvider.Value.RegisterWidget("checkout_completed_top",
                            new PartialViewWidget("_CheckoutCompleted", completedNote, "Smartstore.AmazonPay"));
                    }
                    else if (completedNote.HasValue())
                    {
                        _services.Notifier.Information(completedNote);
                    }
                }

                await next();
                return;
            }

            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<AmazonPayCheckoutState>();

            if (state.SessionId.HasValue() && IsAmazonPaySelected())
            {
                if (action.EqualsNoCase(nameof(CheckoutController.PaymentMethod)))
                {
                    context.Result = new RedirectResult(_urlHelper.Value.Action(nameof(CheckoutController.Confirm), "Checkout"));
                    return;
                }
                else if (action.EqualsNoCase(nameof(CheckoutController.Confirm)))
                {
                    _widgetProvider.Value.RegisterWidget("end",
                        new PartialViewWidget("_CheckoutConfirm", state, "Smartstore.AmazonPay"));
                }
                else
                {
                    _widgetProvider.Value.RegisterWidget("end",
                        new PartialViewWidget("_CheckoutNavigation", state, "Smartstore.AmazonPay"));
                }
            }

            await next();
        }

        private bool IsAmazonPaySelected()
            => _services.WorkContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod.EqualsNoCase(AmazonPayProvider.SystemName);

        private Task<bool> IsAmazonPayActive()
            => _paymentService.Value.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, null, _services.StoreContext.CurrentStore.Id);
    }
}
