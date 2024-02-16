using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class PaymentMethodRequirement : CheckoutRequirementBase
    {
        private static readonly PaymentMethodType[] _paymentTypes =
        [
            PaymentMethodType.Standard,
            PaymentMethodType.Redirection,
            PaymentMethodType.StandardAndRedirection,
            PaymentMethodType.StandardAndButton
        ];

        private readonly IPaymentService _paymentService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly Lazy<IActionContextAccessor> _actionContextAccessor;
        private readonly PaymentSettings _paymentSettings;

        public PaymentMethodRequirement(
            IPaymentService paymentService,
            IOrderCalculationService orderCalculationService,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            Lazy<IActionContextAccessor> actionContextAccessor,
            PaymentSettings paymentSettings)
            : base(httpContextAccessor)
        {
            _paymentService = paymentService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _actionContextAccessor = actionContextAccessor;
            _paymentSettings = paymentSettings;
        }

        public override int Order => 40;

        protected override RedirectToActionResult FulfillResult
            => CheckoutWorkflow.RedirectToCheckout("PaymentMethod");

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart, object model = null)
        {
            var state = _checkoutStateAccessor.CheckoutState;
            var attributes = cart.Customer.GenericAttributes;

            if (model != null 
                && model is string paymentMethod 
                && IsSameRoute(HttpMethods.Post, "SelectPaymentMethod"))
            {
                var provider = await _paymentService.LoadPaymentProviderBySystemNameAsync(paymentMethod, true, cart.StoreId);
                if (provider != null)
                {
                    attributes.SelectedPaymentMethod = paymentMethod;
                    await attributes.SaveChangesAsync();

                    var ctx = _httpContextAccessor.HttpContext;
                    var form = ctx.Request.Form;
                    if (form != null)
                    {
                        // Save payment data so that the user must not re-enter it.
                        foreach (var pair in form)
                        {
                            var v = pair.Value;
                            state.PaymentData[pair.Key] = v.Count == 2 && v[0] != null && v[0] == "true"
                                ? "true"
                                : v.ToString();
                        }
                    }

                    // Validate payment data.
                    var validationResult = await provider.Value.ValidatePaymentDataAsync(form);
                    if (validationResult.IsValid)
                    {
                        var paymentInfo = await provider.Value.GetPaymentInfoAsync(form);
                        ctx.Session.TrySetObject(CheckoutState.OrderPaymentInfoName, paymentInfo);
                        state.PaymentSummary = await provider.Value.GetPaymentSummaryAsync();
                    }
                    else
                    {
                        // TODO: (mg)(quick-checkout) we need to return a second value here
                        // that allows to break all further requirements check.

                        var modelState = _actionContextAccessor.Value.ActionContext.ModelState;
                        validationResult.AddToModelState(modelState);
                    }

                    // INFO: we must return "true" in case of a model state error (invalid payment data).
                    // Otherwise we will be redirected to "GET PaymentMethod" and model state errors are lost.
                    return true;
                }

                return false;
            }

            if (attributes.SelectedPaymentMethod.HasValue() 
                || !state.IsPaymentRequired 
                || state.IsPaymentSelectionSkipped)
            {
                return true;
            }

            Money? shoppingCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);
            state.IsPaymentRequired = shoppingCartTotal.GetValueOrDefault() != decimal.Zero;
            
            if (!state.IsPaymentRequired)
            {
                state.IsPaymentSelectionSkipped = true;
                return true;
            }

            // TODO: (mg)(quick-checkout) perf: LoadActivePaymentProvidersAsync called too often (IsFulfilledAsync, AdvanceAsync, CheckoutPaymentMethodMapper).
            // TODO: (mg)(quick-checkout) perf: CheckoutPaymentMethodModel.SkippedSelectShipping is never used!

            var providers = await _paymentService.LoadActivePaymentProvidersAsync(cart, cart.StoreId, _paymentTypes);
            if (cart.ContainsRecurringItem())
            {
                providers = providers.Where(x => x.Value.RecurringPaymentType > RecurringPaymentType.NotSupported);
            }

            var paymentMethods = providers.ToList();

            state.CustomProperties["HasOnlyOneActivePaymentMethod"] = paymentMethods.Count == 1;
            state.IsPaymentSelectionSkipped = _paymentSettings.BypassPaymentMethodSelectionIfOnlyOne 
                && paymentMethods.Count == 1
                && !paymentMethods[0].Value.RequiresInteraction;

            if (state.IsPaymentSelectionSkipped)
            {
                attributes.SelectedPaymentMethod = paymentMethods[0].Metadata.SystemName;
                await attributes.SaveChangesAsync();
            }

            return attributes.SelectedPaymentMethod.HasValue();
        }
    }
}
