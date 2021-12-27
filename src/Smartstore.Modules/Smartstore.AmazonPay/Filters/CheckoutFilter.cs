using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.AmazonPay.Components;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Filters
{
    public class CheckoutFilter : IAsyncActionFilter
    {
        private static readonly string[] _skipActions = new[] { "BillingAddress", "ShippingAddress", "PaymentMethod" };

        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public CheckoutFilter(
            IStoreContext storeContext,
            IWorkContext workContext,
            Lazy<IPaymentService> paymentService,
            Lazy<IUrlHelper> urlHelper,
            Lazy<IWidgetProvider> widgetProvider,
            ICheckoutStateAccessor checkoutStateAccessor)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _paymentService = paymentService;
            _urlHelper = urlHelper;
            _widgetProvider = widgetProvider;
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Result.IsHtmlViewResult() &&
                _workContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod.EqualsNoCase(AmazonPayProvider.SystemName))
            {
                if (await _paymentService.Value.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, null, _storeContext.CurrentStore.Id))
                {
                    var action = context.RouteData.Values.GetActionName();

                    if (_skipActions.Contains(action, StringComparer.OrdinalIgnoreCase))
                    {
                        context.Result = new RedirectResult(_urlHelper.Value.Action(nameof(CheckoutController.Confirm), "Checkout"));
                    }
                    else if (action.EqualsNoCase(nameof(CheckoutController.Confirm)))
                    {
                        if (_checkoutStateAccessor.CheckoutState?.CustomProperties?.Get(AmazonPayProvider.CheckoutStateKey) is AmazonPayCheckoutState state &&
                            state.CheckoutSessionId.HasValue())
                        {
                            _widgetProvider.Value.RegisterWidget("end",
                                new ComponentWidgetInvoker(typeof(ConfirmOrderViewComponent), new { state }));

                            // TODO: (mg) (core) Use partial view widget with AmazonPayCheckoutState as model. Inject AmazonPaySettings in view.
                            //_widgetProvider.Value.RegisterWidget("end",
                            //    new PartialViewWidgetInvoker("ConfirmOrder", state, AmazonPayProvider.SystemName));
                        }
                    }
                }
            }

            await next();
        }
    }
}
