using System.Diagnostics;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Smartstore.Engine;
using Smartstore.Microsoft.Auth;

namespace Smartstore.Microsoft
{
    internal sealed class MicrosoftAccountOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<MicrosoftAccountOptions>
    {
        private readonly IApplicationContext _appContext;

        public MicrosoftAccountOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(AuthenticationOptions options)
        {
            // Register the OpenID Connect client handler in the authentication handlers collection.
            options.AddScheme(MicrosoftAccountDefaults.AuthenticationScheme, builder =>
            {
                builder.DisplayName = "Microsoft";
                builder.HandlerType = typeof(MicrosoftAccountHandler);
            });
        }

        public void Configure(string name, MicrosoftAccountOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (name.HasValue() && !string.Equals(name, MicrosoftAccountDefaults.AuthenticationScheme))
            {
                return;
            }

            var settings = _appContext.Services.Resolve<MicrosoftExternalAuthSettings>();
            options.ClientId = settings.ClientKeyIdentifier;
            options.ClientSecret = settings.ClientSecret;

            options.Events = new OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    var errorUrl = $"/identity/externalerrorcallback?provider=microsoft&errorMessage={context.Failure.Message.UrlEncode()}";
                    context.Response.Redirect(errorUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        }

        public void Configure(MicrosoftAccountOptions options)
            => Debug.Fail("This infrastructure method shouldn't be called.");
    }
}
