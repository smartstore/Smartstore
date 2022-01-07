using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Smartstore.AmazonPay.Services
{
    public class AmazonPaySignInHandler : AuthenticationHandler<AmazonPaySignInOptions>
    {
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
            var client = Context.GetAmazonPayApiClient(Options.StoreId);
            var response = client.GetBuyer(Options.BuyerToken);

            if (response.Success)
            {
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

                var identity = new ClaimsIdentity(claims, ClaimsIssuer);

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

        //protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        //{
        //    return Context.ChallengeAsync(properties);
        //}
    }
}
