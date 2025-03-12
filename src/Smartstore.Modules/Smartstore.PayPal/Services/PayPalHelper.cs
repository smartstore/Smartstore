using Newtonsoft.Json.Serialization;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Http;
using Smartstore.PayPal.Client.Messages;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal.Services
{
    public class PayPalHelper : ICookiePublisher
    {
        private static JsonSerializerSettings _serializerSettings;
        
        static PayPalHelper()
        {
            _serializerSettings = JsonConvert.DefaultSettings();
            _serializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            _serializerSettings.ContractResolver = new SmartContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };
        }

        public static JsonSerializerSettings SerializerSettings 
        {
            get => _serializerSettings;
        }

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

        public static void HandleException(Exception ex, Localizer T = null)
        {
            var exceptionMessage = JsonConvert.DeserializeObject<ExceptionMessage>(ex.Message, SerializerSettings);

            foreach (var detail in exceptionMessage.Details)
            {
                switch (detail.Issue)
                {
                    case "PAYER_ACTION_REQUIRED":
                        // Redirect to PayPal for user action.
                        var redirectUrl = exceptionMessage.Links.FirstOrDefault(x => x.Rel == "payer-action")?.Href;

                        throw new PaymentException(detail.Description)
                        {
                            RedirectRoute = redirectUrl
                        };

                    case "COUNTRY_NOT_SUPPORTED_BY_PAYMENT_SOURCE":
                        throw new PaymentException(detail.Description)
                        {
                            RedirectRoute = new RouteInfo(nameof(CheckoutController.PaymentMethod), "Checkout", (object)null)
                        };

                    case "BILLING_ADDRESS_INVALID":
                        throw new PaymentException(detail.Description)
                        {
                            RedirectRoute = new RouteInfo(nameof(CheckoutController.BillingAddress), "Checkout", (object)null)
                        };

                    case "PAYMENT_SOURCE_INFO_CANNOT_BE_VERIFIED":
                        throw new PaymentException(T("Plugins.Smartstore.PayPal.PaymentSourceCouldNotBeVerified"))
                        {
                            RedirectRoute = new RouteInfo(nameof(CheckoutController.Confirm), "Checkout", (object)null)
                        };
                }
            }
        }

        public static bool IsCartRoute(string routeIdent)
        {
            return routeIdent == "ShoppingCart.Cart" || routeIdent == "ShoppingCart.UpdateCartItem";
        }
    }
}