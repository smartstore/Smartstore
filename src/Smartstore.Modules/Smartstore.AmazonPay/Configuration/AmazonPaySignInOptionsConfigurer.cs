using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Stores;

namespace Smartstore.AmazonPay
{
    public class AmazonPaySignInOptions : AuthenticationSchemeOptions /*OAuthOptions*/
    {
        public int StoreId { get; set; }
        public string BuyerToken { get; set; }

        public override void Validate()
        {
            if (BuyerToken.IsEmpty())
            {
                throw new ArgumentException("Missing buyer token for sign-in with Amazon Pay.");
            }

            base.Validate();
        }
    }

    internal sealed class AmazonPaySignInOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<AmazonPaySignInOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AmazonPaySignInOptionsConfigurer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Configure(AuthenticationOptions options)
        {
            options.AddScheme(AmazonPaySignInHandler.SchemeName, builder =>
            {
                builder.DisplayName = "Amazon Pay";
                builder.HandlerType = typeof(AmazonPaySignInHandler);
            });
        }

        public void Configure(string name, AmazonPaySignInOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (name.HasValue() && !name.EqualsNoCase(AmazonPaySignInHandler.SchemeName))
            {
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var storeContext = httpContext.RequestServices.GetService<IStoreContext>();

            options.StoreId = storeContext.CurrentStore.Id;
            options.BuyerToken = httpContext.Session.GetString("AmazonPayBuyerToken");

            options.Events = new OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/identity/externalerrorcallback?provider=facebook&errorMessage=" + context.Failure.Message.UrlEncode());
                    context.HandleResponse();

                    return Task.CompletedTask;
                }
            };
        }

        public void Configure(AmazonPaySignInOptions options)
            => Debug.Fail("This infrastructure method should not be called.");
    }
}
