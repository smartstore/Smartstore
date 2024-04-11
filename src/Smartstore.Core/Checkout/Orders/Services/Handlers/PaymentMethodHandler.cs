using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(40, CheckoutActionNames.PaymentMethod)]
    public class PaymentMethodHandler : ICheckoutHandler
    {
        private readonly SmartDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly PaymentSettings _paymentSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public PaymentMethodHandler(
            SmartDbContext db,
            IPaymentService paymentService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            PaymentSettings paymentSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
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
                    var errors = validationResult.Errors
                        .Select(x => new CheckoutError(x.PropertyName, x.ErrorMessage))
                        .ToArray();

                    return new(false, errors);
                }
            }

            var cartTotal = (Money?)await _orderCalculationService.GetShoppingCartTotalAsync(cart, false);
            state.IsPaymentRequired = cartTotal.GetValueOrDefault() != decimal.Zero;

            if (state.IsPaymentRequired)
            {
                if (_paymentSettings.SkipPaymentSelectionIfSingleOption)
                {
                    providers ??= await GetPaymentMethods(cart);

                    state.CustomProperties["HasOnlyOneActivePaymentMethod"] = providers.Count == 1;
                    state.IsPaymentSelectionSkipped = providers.Count == 1 && !providers[0].Value.RequiresPaymentSelection;

                    if (state.IsPaymentSelectionSkipped)
                    {
                        ga.SelectedPaymentMethod  = providers[0].Metadata.SystemName;
                        await ga.SaveChangesAsync();
                    }
                }
            }
            else
            {
                state.IsPaymentSelectionSkipped = true;
            }

            // INFO: "skip" is only set to "true" if the payment selection is always skipped without any exception.
            var skip = state.IsPaymentSelectionSkipped;

            if (_shoppingCartSettings.QuickCheckoutEnabled 
                && state.IsPaymentRequired
                && ga.SelectedPaymentMethod.IsEmpty())
            {
                providers ??= await GetPaymentMethods(cart);

                var preferredMethod = ga.PreferredPaymentMethod;
                var paymentMethod = preferredMethod.HasValue() ? providers.FirstOrDefault(x => x.Metadata.SystemName.EqualsNoCase(preferredMethod))?.Value : null;

                if (paymentMethod != null)
                {
                    ga.SelectedPaymentMethod = preferredMethod;
                    await ga.SaveChangesAsync();

                    if (paymentMethod.RequiresInteraction && !paymentMethod.RequiresPaymentSelection)
                    {
                        // Get payment data.
                        var lastOrder = await _db.Orders
                            .Where(x => x.PaymentMethodSystemName == preferredMethod)
                            .ApplyStandardFilter(cart.Customer.Id, cart.StoreId)
                            .FirstOrDefaultAsync();
                        if (lastOrder != null)
                        {
                            var request = await paymentMethod.CreateProcessPaymentRequestAsync(cart, lastOrder);
                            if (request != null)
                            {
                                context.HttpContext.Session.TrySetObject(CheckoutState.OrderPaymentInfoName, request);
                                state.PaymentSummary = await paymentMethod.GetPaymentSummaryAsync();
                            }
                        }
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
