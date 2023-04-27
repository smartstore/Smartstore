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

        public Task<bool> IsCreditCardActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalCreditCard", null, _services.StoreContext.CurrentStore.Id);

        public Task<bool> IsPayLaterActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalPayLater", null, _services.StoreContext.CurrentStore.Id);

        public Task<bool> IsSepaActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalSepa", null, _services.StoreContext.CurrentStore.Id);

        public Task<bool> IsGiropayActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalGiropay", null, _services.StoreContext.CurrentStore.Id);

        public Task<bool> IsSofortActiveAsync()
            => _paymentService.IsPaymentMethodActiveAsync("Payments.PayPalSofort", null, _services.StoreContext.CurrentStore.Id);

        // TODO: (mh) (core) Add the others Bancontact, Blik, Eps,  Ideal, MercadoPago, P24, Venmo
    }
}
