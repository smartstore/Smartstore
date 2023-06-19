using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.PayPal.Services
{
    public class PayPalHelper : ICookiePublisher
    {
        private readonly IStoreContext _storeContext;
        private readonly IPaymentService _paymentService;
        private readonly Localizer T;

        public PayPalHelper(IStoreContext storeContext, IPaymentService paymentService, Localizer localizer)
        {
            _storeContext = storeContext;
            _paymentService = paymentService;
            T = localizer;
        }

        public Task<bool> IsProviderEnabledAsync(string systemName)
            => _paymentService.IsPaymentProviderEnabledAsync(systemName, _storeContext.CurrentStore.Id);

        public Task<bool> IsProviderActiveAsync(string systemName)
            => _paymentService.IsPaymentProviderActiveAsync(systemName, null, _storeContext.CurrentStore.Id);

        public Task<bool> IsAnyProviderEnabledAsync(params string[] systemNames)
        {
            Guard.NotNull(systemNames);
            return systemNames.AnyAsync(x => _paymentService.IsPaymentProviderEnabledAsync(x, _storeContext.CurrentStore.Id));
        }

        public Task<bool> IsAnyProviderActiveAsync(params string[] systemNames)
        {
            Guard.NotNull(systemNames);
            return systemNames.AnyAsync(x => _paymentService.IsPaymentProviderActiveAsync(x, null, _storeContext.CurrentStore.Id));
        }

        public async Task<IEnumerable<CookieInfo>> GetCookieInfosAsync()
        {
            // INFO: APMs don't need cookies as everything on page is handled via API requests.
            // The pages to which the customer is redirected when using APMs must handle cookie consent themsleves.
            if (await IsAnyProviderEnabledAsync(
                PayPalConstants.Standard,
                PayPalConstants.PayUponInvoice,
                PayPalConstants.CreditCard,
                PayPalConstants.PayLater,
                PayPalConstants.Sepa))
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