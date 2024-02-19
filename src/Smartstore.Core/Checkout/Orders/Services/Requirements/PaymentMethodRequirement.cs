using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class PaymentMethodRequirement : CheckoutRequirementBase
    {
        const string ActionName = "PaymentMethod";

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
        private readonly PaymentSettings _paymentSettings;

        public PaymentMethodRequirement(
            IPaymentService paymentService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            PaymentSettings paymentSettings)
            : base(httpContextAccessor)
        {
            _paymentService = paymentService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _paymentSettings = paymentSettings;
        }

        public override int Order => 40;

        protected override RedirectToActionResult FulfillResult
            => CheckoutWorkflow.RedirectToCheckout(ActionName);

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart, IList<CheckoutWorkflowError> errors, object model = null)
        {
            var state = _checkoutStateAccessor.CheckoutState;
            var attributes = cart.Customer.GenericAttributes;

            if (model != null 
                && model is string paymentMethod 
                && IsSameRoute(HttpMethods.Post, ActionName))
            {
                var provider = await _paymentService.LoadPaymentProviderBySystemNameAsync(paymentMethod, true, cart.StoreId);
                if (provider == null)
                {
                    return false;
                }

                attributes.SelectedPaymentMethod = paymentMethod;
                await attributes.SaveChangesAsync();

                var form = HttpContext.Request.Form;
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
                    HttpContext.Session.TrySetObject(CheckoutState.OrderPaymentInfoName, paymentInfo);
                    state.PaymentSummary = await provider.Value.GetPaymentSummaryAsync();
                }
                else
                {
                    validationResult.Errors.Each(x => errors.Add(new(x.PropertyName, x.ErrorMessage)));
                }

                return validationResult.IsValid;
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
