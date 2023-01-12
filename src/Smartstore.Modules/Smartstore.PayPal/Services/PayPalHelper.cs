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

        public Task<bool> IsPaymentMethodActiveAsync(string systemName)
            => _paymentService.IsPaymentMethodActiveAsync(systemName, null, _services.StoreContext.CurrentStore.Id);
    }
}
