using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;

namespace Smartstore.PayPal.Services
{
    public class PayPalHelper : ICookiePublisher
    {
        private readonly ICommonServices _services;
        private readonly IPaymentService _paymentService;

        public PayPalHelper(ICommonServices services, IPaymentService paymentService)
        {
            _services = services;
            _paymentService = paymentService;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

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

        public async Task<bool> IsAnyMethodActiveAsync(params string[] providerSystemNames)
        {
            Guard.NotEmpty(providerSystemNames);

            var activePaymentMethods = await _paymentService.LoadActivePaymentMethodsAsync(null, _services.StoreContext.CurrentStore.Id);
            return activePaymentMethods.Any(x => providerSystemNames.Contains(x.Metadata.SystemName));
        }

        // TODO: (mh) (core) Add the others Bancontact, Blik, Eps,  Ideal, MercadoPago, P24, Venmo

        public async Task<IEnumerable<CookieInfo>> GetCookieInfosAsync()
        {
            // INFO: APMs don't need cookies as everything on page is handled via API requests.
            // The pages to which the customer is redirected when using APMs must handle cookie consent themsleves.
            if (await IsAnyMethodActiveAsync(
                "Payments.PayPalStandard",
                "Payments.PayPalPayUponInvoice",
                "Payments.PayPalCreditCard",
                "Payments.PayPalPayLater",
                "Payments.PayPalSepa"))
            {
                var cookieInfo = new CookieInfo
                {
                    Name = T("Plugins.FriendlyName.Smartstore.PayPal"),
                    Description = T("Plugins.Smartstore.PayPal.CookieInfo"),
                    CookieType = CookieType.Required
                };

                return new List<CookieInfo> { cookieInfo }.AsEnumerable();
            }

            return null;
        }
    }
}
