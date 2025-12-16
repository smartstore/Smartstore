using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Filters
{
    public class CheckoutFilter : IAsyncActionFilter
    {
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly INotifier _notifier;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly OrderSettings _orderSettings;
        private readonly Localizer T;

        public CheckoutFilter(
            IStoreContext storeContext,
            IWorkContext workContext,
            INotifier notifier,
            Lazy<IPaymentService> paymentService,
            Lazy<IUrlHelper> urlHelper,
            Lazy<IWidgetProvider> widgetProvider,
            ICheckoutStateAccessor checkoutStateAccessor,
            OrderSettings orderSettings,
            Localizer localizer)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _notifier = notifier;
            _paymentService = paymentService;
            _urlHelper = urlHelper;
            _widgetProvider = widgetProvider;
            _checkoutStateAccessor = checkoutStateAccessor;
            _orderSettings = orderSettings;
            T = localizer;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var action = context.RouteData.Values.GetActionName();

            if (action.EqualsNoCase(nameof(CheckoutController.Completed)))
            {
                var session = context.HttpContext.Session;
                var responseStatus = session.GetString("AmazonPayResponseStatus");
                if (responseStatus.HasValue())
                {
                    if (await IsAmazonPayActive())
                    {
                        // 202 (Accepted): authorization is pending.
                        string completedNote = responseStatus == "202"
                            ? T("Plugins.Payments.AmazonPay.AsyncPaymentAuthorizationNote")
                            : string.Empty;

                        if (!_orderSettings.DisableOrderCompletedPage)
                        {
                            _widgetProvider.Value.RegisterWidget("checkout_completed_top",
                                new PartialViewWidget("_CheckoutCompleted", completedNote, "Smartstore.AmazonPay"));
                        }
                        else if (completedNote.HasValue())
                        {
                            _notifier.Information(completedNote);
                        }
                    }

                    session.TryRemove("AmazonPayResponseStatus");
                }

                await next();
                return;
            }

            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<AmazonPayCheckoutState>();

            if (state.SessionId.HasValue() && IsAmazonPaySelected() && await IsAmazonPayActive())
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
            => _workContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod.EqualsNoCase(AmazonPayProvider.SystemName);

        private Task<bool> IsAmazonPayActive()
            => _paymentService.Value.IsPaymentProviderActiveAsync(AmazonPayProvider.SystemName, null, _storeContext.CurrentStore.Id);
    }
}
