using System.Diagnostics;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Smartstore.Engine;
using Smartstore.Google.Auth;

namespace Smartstore.Google
{
    internal sealed class GoogleOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<GoogleOptions>
    {
        private readonly IApplicationContext _appContext;

        public GoogleOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(AuthenticationOptions options)
        {
            // Register the OpenID Connect client handler in the authentication handlers collection.
            options.AddScheme(GoogleDefaults.AuthenticationScheme, builder =>
            {
                builder.DisplayName = "Google";
                builder.HandlerType = typeof(GoogleHandler);
            });
        }

        public void Configure(string name, GoogleOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (name.HasValue() && !string.Equals(name, GoogleDefaults.AuthenticationScheme))
            {
                return;
            }

            var settings = _appContext.Services.Resolve<GoogleExternalAuthSettings>();
            options.ClientId = settings.ConsumerKey;
            options.ClientSecret = settings.ConsumerSecret;

            options.Events = new OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    var errorUrl = context.Request.PathBase.Value + $"/identity/externalerrorcallback?provider=google&errorMessage={context.Failure.Message}";
                    context.Response.Redirect(errorUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        }

        public void Configure(GoogleOptions options)
            => Debug.Fail("This infrastructure method shouldn't be called.");
    }
}
