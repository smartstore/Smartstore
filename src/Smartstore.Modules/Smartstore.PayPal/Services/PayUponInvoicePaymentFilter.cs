using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;

namespace Smartstore.PayPal.Services
{
    /// <summary>
    /// Filters out Pay Upon Invoice if the cart total is above upper limit.
    /// </summary>
    public partial class PayUponInvoicePaymentFilter : IPaymentMethodFilter
    {
        private readonly Lazy<ISettingFactory> _settingFactory;
        private readonly IOrderCalculationService _orderCalculationService;
        
        public PayUponInvoicePaymentFilter(Lazy<ISettingFactory> settingFactory, IOrderCalculationService orderCalculationService)
        {
            _settingFactory = settingFactory;
            _orderCalculationService = orderCalculationService;
        }

        public async Task<bool> IsExcludedAsync(PaymentFilterRequest request)
        {
            if (request.PaymentMethod.Metadata.SystemName.EqualsNoCase("Payments.PayPalPayUponInvoice"))
            {
                var settings = await _settingFactory.Value.LoadSettingsAsync<PayPalSettings>(request.StoreId);

                // If client id or secret haven't been configured yet, don't render payment method.
                if (!settings.ClientId.HasValue() || !settings.Secret.HasValue())
                {
                    return true;
                }

                if (request.Cart != null)
                {
                    // Do not render Pay Upon Invoice if order total is above limit.
                    var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(request.Cart);
                    if (cartTotal.Total.HasValue && cartTotal.Total.Value.Amount >= settings.PayUponInvoiceLimit)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}