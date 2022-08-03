using System.Diagnostics;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Smartstore.Engine;
using Smartstore.Facebook.Auth;

namespace Smartstore.Facebook
{
    internal sealed class FacebookOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<FacebookOptions>
    {
        private readonly IApplicationContext _appContext;

        public FacebookOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(AuthenticationOptions options)
        {
            // Register the OpenID Connect client handler in the authentication handlers collection.
            options.AddScheme(FacebookDefaults.AuthenticationScheme, builder =>
            {
                builder.DisplayName = "Facebook";
                builder.HandlerType = typeof(FacebookHandler);
            });
        }

        public void Configure(string name, FacebookOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (name.HasValue() && !string.Equals(name, FacebookDefaults.AuthenticationScheme))
            {
                return;
            }

            var settings = _appContext.Services.Resolve<FacebookExternalAuthSettings>();
            options.AppId = settings.ClientKeyIdentifier;
            options.AppSecret = settings.ClientSecret;

            options.Events = new OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    var errorUrl = $"/identity/externalerrorcallback?provider=facebook&errorMessage={context.Failure.Message.UrlEncode()}";
                    context.Response.Redirect(errorUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        }

        public void Configure(FacebookOptions options)
            => Debug.Fail("This infrastructure method shouldn't be called.");
    }
}
