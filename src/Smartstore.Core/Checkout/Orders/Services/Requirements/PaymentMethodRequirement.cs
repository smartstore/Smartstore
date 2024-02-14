using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class PaymentMethodRequirement : ICheckoutRequirement
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
        private readonly PaymentSettings _paymentSettings;

        public PaymentMethodRequirement(
            IPaymentService paymentService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor, 
            PaymentSettings paymentSettings)
        {
            _paymentService = paymentService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _paymentSettings = paymentSettings;
        }

        public static int CheckoutOrder => ShippingMethodRequirement.CheckoutOrder + 10;
        public int Order => CheckoutOrder;

        public IActionResult Fulfill()
            => CheckoutWorkflow.RedirectToCheckout("PaymentMethod");

        public async Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            var state = _checkoutStateAccessor.CheckoutState;
            var attributes = cart.Customer.GenericAttributes;

            if (attributes.SelectedPaymentMethod.HasValue() || !state.IsPaymentRequired || state.IsPaymentSelectionSkipped)
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

            // TODO: (mg)(quick-checkout) perf: LoadActivePaymentProvidersAsync called twice (in CheckoutPaymentMethodMapper and PaymentMethodRequirement).
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
