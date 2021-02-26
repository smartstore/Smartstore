using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Cart.Events
{
    public class ValidatingCartEventConsumer : IConsumer
    {
        // TODO: (ms) (core) OrderProcessingService is missing
        //private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly ICurrencyService _currencyService;        
        private readonly IWorkContext _workContext;

        public ValidatingCartEventConsumer(
          //  IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService,
            ICurrencyService currencyService,
            IWorkContext workContext)
        {
            //_orderProcessingService = orderProcessingService;
            _localizationService = localizationService;
            _currencyService = currencyService;
            _workContext = workContext;
        }

        //public void HandleEvent(ValidatingCartEvent message)
        //{
        //    // Default Order Totals restriction
        //    var roleIds = _workContext.CurrentImpersonator?.GetRoleIds() ?? message.Customer.GetRoleIds();

        //    // Minimum order totals validation
        //    var (isAboveMin, min) = _orderProcessingService.IsAboveOrderTotalMinimum(message.Cart, roleIds);
        //    if (!isAboveMin)
        //    {
        //        min = _currencyService.ConvertFromPrimaryStoreCurrency(min, _workContext.WorkingCurrency);
        //        message.Warnings.Add(string.Format(
        //            _localizationService.GetResource("Checkout.MinOrderSubtotalAmount"),
        //            _currencyService.AsMoney(min, true, displayTax: false))
        //            );

        //        return;
        //    }

        //    // Maximum order totals validation
        //    var (isBelowMax, max) = _orderProcessingService.IsBelowOrderTotalMaximum(message.Cart, roleIds);
        //    if (!isBelowMax)
        //    {
        //        max = _currencyService.ConvertFromPrimaryStoreCurrency(max, _workContext.WorkingCurrency);
        //        message.Warnings.Add(string.Format(
        //           _localizationService.GetResource("Checkout.MaxOrderSubtotalAmount"),
        //           _priceFormatter.FormatPrice(max, true, false))
        //            );
        //    }
        //}
    }
}