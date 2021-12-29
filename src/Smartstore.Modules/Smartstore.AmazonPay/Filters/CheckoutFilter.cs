using System.Linq;
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
        private readonly AmazonPaySettings _settings;
        private readonly OrderSettings _orderSettings;

        public CheckoutFilter(
            ICommonServices services,
            Lazy<IPaymentService> paymentService,
            Lazy<IUrlHelper> urlHelper,
            Lazy<IWidgetProvider> widgetProvider,
            ICheckoutStateAccessor checkoutStateAccessor,
            AmazonPaySettings settings,
            OrderSettings orderSettings)
        {
            _services = services;
            _paymentService = paymentService;
            _urlHelper = urlHelper;
            _widgetProvider = widgetProvider;
            _checkoutStateAccessor = checkoutStateAccessor;
            _settings = settings;
            _orderSettings = orderSettings;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var action = context.RouteData.Values.GetActionName();

            if (_skipActions.Contains(action, StringComparer.OrdinalIgnoreCase))
            {
                if (IsAmazonPaySelected() && await IsAmazonPayActive())
                {
                    context.Result = new RedirectResult(_urlHelper.Value.Action(nameof(CheckoutController.Confirm), "Checkout"));
                }
            }
            else if (action.EqualsNoCase(nameof(CheckoutController.Confirm)))
            {
                if (IsAmazonPaySelected()
                    && await IsAmazonPayActive()
                    && _checkoutStateAccessor.CheckoutState?.CustomProperties?.Get(AmazonPayProvider.CheckoutStateKey) is AmazonPayCheckoutState state
                    && state.CheckoutSessionId.HasValue())
                {
                    _widgetProvider.Value.RegisterWidget("end",
                        new PartialViewWidgetInvoker("_CheckoutConfirm", state, "Smartstore.AmazonPay"));
                }
            }
            else if (action.EqualsNoCase(nameof(CheckoutController.Completed)))
            {
                // TODO: (mg) (core) rework this.
                // ResetCheckoutData was called. Control it via a session object in ProcessPaymentAsync. Only register widget for AmazonPay!
                //var note = context.HttpContext.Session.GetString("AmazonPayCheckoutCompletedNote");

                //if (_orderSettings.DisableOrderCompletedPage && note.HasValue())
                //{
                //    _services.Notifier.Information(note);
                //}
                //else if (_settings.ShowSignoutButton || note.HasValue())
                //{
                //    _widgetProvider.Value.RegisterWidget("checkout_completed_top",
                //        new PartialViewWidgetInvoker("_CheckoutCompleted", note, "Smartstore.AmazonPay"));
                //}
            }

            await next();
        }

        private bool IsAmazonPaySelected()
            => _services.WorkContext.CurrentCustomer.GenericAttributes.SelectedPaymentMethod.EqualsNoCase(AmazonPayProvider.SystemName);

        private Task<bool> IsAmazonPayActive()
            => _paymentService.Value.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, null, _services.StoreContext.CurrentStore.Id);
    }
}
