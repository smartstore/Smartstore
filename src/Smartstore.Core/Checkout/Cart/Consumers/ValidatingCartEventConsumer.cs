using System.Threading.Tasks;
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
            // Default Order Totals restriction
            var roleIds = _workContext.CurrentImpersonator?.GetRoleIds() ?? message.Customer.GetRoleIds();

            // Minimum order totals validation
            var (isAboveMin, min) =  await _orderProcessingService.IsAboveOrderTotalMinimumAsync(message.Cart, roleIds);
            if (!isAboveMin)
            {
                min = _currencyService.ConvertFromPrimaryCurrency(min.Amount, _workContext.WorkingCurrency);
                message.Warnings.Add(string.Format(
                    _localizationService.GetResource("Checkout.MinOrderSubtotalAmount"),
                    min.ToString(true))
                    );

                return;
            }

            // Maximum order totals validation
            var (isBelowMax, max) = await _orderProcessingService.IsBelowOrderTotalMaximumAsync(message.Cart, roleIds);
            if (!isBelowMax)
            {
                max = _currencyService.ConvertFromPrimaryCurrency(max.Amount, _workContext.WorkingCurrency);
                message.Warnings.Add(string.Format(
                    _localizationService.GetResource("Checkout.MaxOrderSubtotalAmount"),
                    max.ToString(true))
                    );
            }
        }
    }
}