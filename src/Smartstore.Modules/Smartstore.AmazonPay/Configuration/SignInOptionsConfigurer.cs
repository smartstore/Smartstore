using System.Diagnostics;
using Amazon.Pay.API.WebStore.Types;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Stores;
using Smartstore.Engine;

namespace Smartstore.AmazonPay
{
    public class SignInOptions : OAuthOptions
    {
        public readonly static SignInScope[] Scopes = new[]
        {
            SignInScope.Name,
            SignInScope.Email,
            //SignInScope.PostalCode, 
            SignInScope.ShippingAddress,
            SignInScope.BillingAddress,
            SignInScope.PhoneNumber
        };

        internal Dictionary<string, string> Res { get; } = new();

        public int StoreId { get; set; }
        public string BuyerToken { get; set; }

        public override void Validate()
        {
            if (BuyerToken.IsEmpty())
            {
                throw new ArgumentException(Res.Get("MissingAccessToken"));
            }

            base.Validate();
        }
    }

    internal sealed class SignInOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<SignInOptions>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SignInOptionsConfigurer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Configure(AuthenticationOptions options)
        {
            options.AddScheme(SignInHandler.SchemeName, builder =>
            {
                builder.DisplayName = "Amazon Pay";
                builder.HandlerType = typeof(SignInHandler);
            });
        }

        public void Configure(string name, SignInOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (name.HasValue() && !name.EqualsNoCase(SignInHandler.SchemeName))
            {
                return;
            }

            // INFO: (mg) (core) AppContext.Services is the ROOT (singleton) container. Never resolve scoped dependencies from it!
            var httpContext = _httpContextAccessor.HttpContext;
            var storeContext = httpContext.RequestServices.GetService<IStoreContext>();
            var T = httpContext.RequestServices.GetService<Localizer>();

            options.StoreId = storeContext.CurrentStore.Id;
            options.BuyerToken = httpContext.Session.GetString("AmazonPayBuyerToken");

            options.Res["MissingAccessToken"] = T("Plugins.Payments.AmazonPay.MissingAccessToken");

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

        public void Configure(SignInOptions options)
            => Debug.Fail("This infrastructure method should not be called.");
    }
}
