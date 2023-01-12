using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;

namespace Smartstore.PayPal.Services
{
    public class PayPalHelper
    {
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;

        public PayPalHelper(ICommonServices services, IPaymentService paymentService)
        {
            _services = services;
            _paymentService = paymentService;
        }

        public Task<bool> IsPayPalStandardActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalStandard", null, _services.StoreContext.CurrentStore.Id);

        public Task<bool> IsPayUponInvoiceActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalPayUponInvoice", null, _services.StoreContext.CurrentStore.Id);
    }
}
