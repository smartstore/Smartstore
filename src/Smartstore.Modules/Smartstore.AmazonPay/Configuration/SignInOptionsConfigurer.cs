using System.Diagnostics;
using Amazon.Pay.API.WebStore.Types;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
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
        private readonly IApplicationContext _appContext;

        public SignInOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
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

            var T = _appContext.Services.Resolve<Localizer>();
            var storeContext = _appContext.Services.Resolve<IStoreContext>();
            var httpContextAccessor = _appContext.Services.Resolve<IHttpContextAccessor>();

            options.StoreId = storeContext.CurrentStore.Id;
            options.BuyerToken = httpContextAccessor.HttpContext.Session.GetString("AmazonPayBuyerToken");

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
