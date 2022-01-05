using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Smartstore.AmazonPay.Services
{
    // TODO: (mg) (core) somehow register this the way IdentityController.ExternalLogin can pick it up.
    public class SignInHandler : AuthenticationHandler<SignInOptions>
    {
        internal static readonly string SchemeName = "AmazonPay.SignIn";

        public SignInHandler(
            IOptionsMonitor<SignInOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder, 
            ISystemClock clock) 
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Scheme.Name.EqualsNoCase(SchemeName))
            {
                var client = await Context.GetAmazonPayApiClientAsync(Options.StoreId);
                var response = client.GetBuyer(Options.BuyerToken);

                if (response.Success)
                {
                    // TODO: (mg) (core) add more claim stuff.
                    // TODO: (mg) (core) ExternalAuthenticationRecord.ExternalIdentifier aka UserLoginInfo.ProviderKey.
                    var claims = new[] 
                    {
                        new Claim(ClaimTypes.Name, response.Name),
                        new Claim(ClaimTypes.Email, response.Email)
                    };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }
                else
                {
                    Logger.LogAmazonPayFailure(null, response);
                }
            }

            return AuthenticateResult.NoResult();
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
