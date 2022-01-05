using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Smartstore.AmazonPay.Services
{
    public class AmazonPaySignInHandler : AuthenticationHandler<AmazonPaySignInOptions>
    {
        internal static readonly string SchemeName = "AmazonPay.SignIn";

        public AmazonPaySignInHandler(
            IOptionsMonitor<AmazonPaySignInOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder, 
            ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Scheme.Name.EqualsNoCase(SchemeName))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var client = Context.GetAmazonPayApiClient(Options.StoreId);
            var response = client.GetBuyer(Options.BuyerToken);

            if (response.Success)
            {
                var (firstName, lastName) = AmazonPayService.GetFirstAndLastName(response.Name);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, response.BuyerId),
                    new Claim(ClaimTypes.Email, response.Email),
                    new Claim(ClaimTypes.Name, response.Name),
                    new Claim(ClaimTypes.GivenName, firstName),
                    new Claim(ClaimTypes.Surname, lastName),
                    //new Claim(ClaimTypes.HomePhone, response.BillingAddress?.PhoneNumber ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            else
            {
                var message = Logger.LogAmazonPayFailure(null, response);

                return Task.FromResult(AuthenticateResult.Fail(message));
            }            
        }
    }

    //public class SignInHandler : OAuthHandler<SignInOptions>
    //{
    //    internal static readonly string SchemeName = "AmazonPay.SignIn";

    //    public SignInHandler(IOptionsMonitor<SignInOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) 
    //        : base(options, logger, encoder, clock)
    //    {
    //    }

    //    public IApplicationContext Application { get; set; }

    //    /// <inheritdoc />
    //    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
    //    {
    //        var httpContextAccessor = Application.Services.Resolve<IHttpContextAccessor>();
    //        var logger = Application.Services.Resolve<ILogger>();

    //        var client = await httpContextAccessor.HttpContext.GetAmazonPayApiClientAsync(Options.StoreId);
    //        var response = client.GetBuyer(Options.BuyerToken);

    //        if (response.Success)
    //        {
    //        }
    //        else
    //        {
    //            logger.LogAmazonPayFailure(null, response);
    //        }

    //        var principal = new ClaimsPrincipal(identity);

    //        return new AuthenticationTicket(principal, Scheme.Name);
    //    }
    //}
}
