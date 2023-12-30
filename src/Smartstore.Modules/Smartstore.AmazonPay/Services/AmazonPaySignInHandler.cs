using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;

namespace Smartstore.AmazonPay.Services
{
    public class AmazonPaySignInOptions : AuthenticationSchemeOptions
    {
        public int? StoreId { get; set; }
        public string BuyerToken { get; set; }
    }

    public class AmazonPaySignInHandler : AuthenticationHandler<AmazonPaySignInOptions>
    {
        private readonly IStoreContext _storeContext;
        private readonly SignInManager<Customer> _signInManager;
        private readonly IProviderManager _providerManager;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;

        public AmazonPaySignInHandler(
            IOptionsMonitor<AmazonPaySignInOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IStoreContext storeContext,
            SignInManager<Customer> signInManager,
            IProviderManager providerManager,
            ExternalAuthenticationSettings externalAuthenticationSettings)
            : base(options, logger, encoder)
        {
            _storeContext = storeContext;
            _signInManager = signInManager;
            _providerManager = providerManager;
            _externalAuthenticationSettings = externalAuthenticationSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var buyerToken = Options.BuyerToken;
            if (buyerToken.IsEmpty() && Context.Request.Query.TryGetValue("buyerToken", out var rawToken))
            {
                buyerToken = rawToken.ToString();
            }

            if (buyerToken.IsEmpty())
            {
                return AuthenticateResult.Fail(T("Plugins.Payments.AmazonPay.MissingAccessToken"));
            }

            // INFO: security. We have to check if the shop really wants to do this authentication
            // because the AmazonPay callback URL can be called by anyone.
            var provider = _providerManager.GetProvider<IExternalAuthenticationMethod>(AmazonPaySignInProvider.SystemName);
            if (!provider.IsMethodActive(_externalAuthenticationSettings))
            {
                return AuthenticateResult.Fail("External login with AmazonPay is deactivated.");
            }

            var storeId = Options.StoreId ?? _storeContext.CurrentStore.Id;
            var client = Context.GetAmazonPayApiClient(storeId);
            var response = client.GetBuyer(buyerToken);

            if (response.Success)
            {
                var properties = _signInManager.ConfigureExternalAuthenticationProperties(Scheme.Name, null);

                // INFO: IsPersistent must be false. SignInManager.GetExternalLoginInfoAsync -> AuthenticationHttpContextExtensions.AuthenticateAsync
                // would return a "Ticket expired" failure otherwise.
                properties.IsPersistent = false;

                var (firstName, lastName) = AmazonPayService.GetFirstAndLastName(response.Name);
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, response.BuyerId, ClaimValueTypes.String, ClaimsIssuer),
                    new Claim(ClaimTypes.Email, response.Email, ClaimValueTypes.String, ClaimsIssuer),
                    new Claim(ClaimTypes.Name, response.Name, ClaimValueTypes.String, ClaimsIssuer),
                    new Claim(ClaimTypes.GivenName, firstName, ClaimValueTypes.String, ClaimsIssuer),
                    new Claim(ClaimTypes.Surname, lastName, ClaimValueTypes.String, ClaimsIssuer),
                    //new Claim(ClaimTypes.HomePhone, response.BillingAddress?.PhoneNumber ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, properties, Scheme.Name);

                await Context.SignInAsync(null, principal, ticket.Properties);

                return AuthenticateResult.Success(ticket);
            }
            else
            {
                var message = T("Plugins.Payments.AmazonPay.SignInFailureMessage");
                Logger.Log(response, message);

                return AuthenticateResult.Fail(message);
            }
        }
    }
}
