using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Cart
{
    internal class ValidatingCartEventConsumer : IConsumer
    {
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly ICurrencyService _currencyService;
        private readonly IWorkContext _workContext;

        public ValidatingCartEventConsumer(
            IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService,
            ICurrencyService currencyService,
            IWorkContext workContext)
        {
            _orderProcessingService = orderProcessingService;
            _localizationService = localizationService;
            _currencyService = currencyService;
            _workContext = workContext;
        }

        public async Task HandleEventAsync(ValidatingCartEvent message)
        {
            var cart = message.Cart;
            if (!cart.HasItems || cart.CartType == ShoppingCartType.Wishlist)
            {
                return;
            }

            // Order total validation.
            var roleMappings = _workContext.CurrentImpersonator?.CustomerRoleMappings ?? cart.Customer.CustomerRoleMappings;
            var result = await _orderProcessingService.ValidateOrderTotalAsync(cart, roleMappings.Select(x => x.CustomerRole).ToArray());

            if (!result.IsAboveMinimum)
            {
                var convertedMin = _currencyService.ConvertFromPrimaryCurrency(result.OrderTotalMinimum, _workContext.WorkingCurrency);

                message.Warnings.Add(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount").FormatInvariant(convertedMin.ToString(true)));
            }

            if (!result.IsBelowMaximum)
            {
                var convertedMax = _currencyService.ConvertFromPrimaryCurrency(result.OrderTotalMaximum, _workContext.WorkingCurrency);

                message.Warnings.Add(_localizationService.GetResource("Checkout.MaxOrderSubtotalAmount").FormatInvariant(convertedMax.ToString(true)));
            }
        }
    }
}