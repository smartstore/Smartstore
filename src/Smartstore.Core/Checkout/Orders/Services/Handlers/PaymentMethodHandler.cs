using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(40, CheckoutActionNames.PaymentMethod)]
    public class PaymentMethodHandler : ICheckoutHandler
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly PaymentSettings _paymentSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public PaymentMethodHandler(
            IPaymentService paymentService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            PaymentSettings paymentSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _paymentService = paymentService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _paymentSettings = paymentSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<CheckoutResult> ProcessAsync(CheckoutContext context)
        {
            var state = _checkoutStateAccessor.CheckoutState;
            var cart = context.Cart;
            var ga = cart.Customer.GenericAttributes;
            List<Provider<IPaymentMethod>> providers = null;

            if (context.Model != null 
                && context.Model is string systemName 
                && context.IsCurrentRoute(HttpMethods.Post, CheckoutActionNames.PaymentMethod))
            {
                var provider = await _paymentService.LoadPaymentProviderBySystemNameAsync(systemName, true, cart.StoreId);
                if (provider == null)
                {
                    return new(false);
                }

                var oldPreferredPaymentMethod = ga.PreferredPaymentMethod;

                // SelectedPaymentMethod must be set before validation.
                ga.SelectedPaymentMethod = systemName;
                ga.PreferredPaymentMethod = systemName;
                await ga.SaveChangesAsync();

                var form = context.HttpContext.Request.Form;
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
                    context.HttpContext.Session.TrySetObject(CheckoutState.OrderPaymentInfoName, paymentInfo);
                    state.PaymentSummary = await provider.Value.GetPaymentSummaryAsync();

                    return new(true);
                }
                else
                {
                    if (!ga.PreferredPaymentMethod.EqualsNoCase(oldPreferredPaymentMethod))
                    {
                        // Reset.
                        ga.PreferredPaymentMethod = oldPreferredPaymentMethod;
                        await ga.SaveChangesAsync();
                    }

                    var errors = validationResult.Errors
                        .Select(x => new CheckoutError(x.PropertyName, x.ErrorMessage))
                        .ToArray();

                    return new(false, errors);
                }
            }

            var cartTotal = (Money?)await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);
            state.IsPaymentRequired = cartTotal.GetValueOrDefault() != decimal.Zero;

            if (!state.IsPaymentRequired)
            {
                state.IsPaymentSelectionSkipped = true;

                if (ga.SelectedPaymentMethod.HasValue())
                {
                    ga.SelectedPaymentMethod = null;
                    await ga.SaveChangesAsync();
                }

                return new(true, null, true);
            }

            state.CustomProperties["HasOnlyOneActivePaymentMethod"] = false;
            state.IsPaymentSelectionSkipped = false;

            if (_paymentSettings.SkipPaymentSelectionIfSingleOption)
            {
                providers ??= await GetPaymentMethods(cart);
                if (providers.Count == 1)
                {
                    var pm = providers[0].Value;

                    // Offer link to payment page if the one payment method requires any interaction.
                    // Customer must be able to correct his input.
                    state.CustomProperties["HasOnlyOneActivePaymentMethod"] = !pm.RequiresInteraction;

                    // Only skip payment page if the payment selection can always be skipped without any exception.
                    state.IsPaymentSelectionSkipped = !pm.RequiresInteraction && !pm.RequiresPaymentSelection;

                    if (state.IsPaymentSelectionSkipped)
                    {
                        ga.SelectedPaymentMethod = providers[0].Metadata.SystemName;
                        await ga.SaveChangesAsync();
                    }
                }
            }

            var skip = state.IsPaymentSelectionSkipped;

            if (_shoppingCartSettings.QuickCheckoutEnabled && ga.SelectedPaymentMethod.IsEmpty())
            {
                // Apply preferred method if it does not require payment selection page.
                providers ??= await GetPaymentMethods(cart);

                var applyMethod = true;
                var preferredMethodName = ga.PreferredPaymentMethod;
                var preferredMethod = preferredMethodName.HasValue() 
                    ? providers.FirstOrDefault(x => x.Metadata.SystemName.EqualsNoCase(preferredMethodName) && !x.Value.RequiresPaymentSelection)?.Value 
                    : null;

                if (preferredMethod != null)
                {
                    if (preferredMethod.RequiresInteraction)
                    {
                        // Call payment provider to get payment data from last order.
                        var request = await preferredMethod.CreateProcessPaymentRequestAsync(cart);
                        applyMethod = request != null;

                        if (applyMethod)
                        {
                            context.HttpContext.Session.TrySetObject(CheckoutState.OrderPaymentInfoName, request);
                            state.PaymentSummary = await preferredMethod.GetPaymentSummaryAsync();
                        }
                    }

                    if (applyMethod)
                    {
                        // The preferred payment method can be applied automatically.
                        ga.SelectedPaymentMethod = preferredMethodName;
                        await ga.SaveChangesAsync();
                    }
                }
            }

            return new(ga.SelectedPaymentMethod.HasValue(), null, skip);
        }

        private async Task<List<Provider<IPaymentMethod>>> GetPaymentMethods(ShoppingCart cart)
        {
            var providers = await _paymentService.LoadActivePaymentProvidersAsync(cart, cart.StoreId);
            
            if (cart.ContainsRecurringItem())
            {
                providers = providers.Where(x => x.Value.RecurringPaymentType > RecurringPaymentType.NotSupported);
            }

            return providers.ToList();
        }
    }
}
