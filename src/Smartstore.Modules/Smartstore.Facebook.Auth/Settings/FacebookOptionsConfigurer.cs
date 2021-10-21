using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Facebook.Auth;

namespace Smartstore.Facebook.Bootstrapping
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
            var settings = _appContext.Services.Resolve<FacebookExternalAuthSettings>();

            if (settings.ClientKeyIdentifier.HasValue() && settings.ClientSecret.HasValue())
            {
                // Register the OpenID Connect client handler in the authentication handlers collection.
                options.AddScheme(FacebookDefaults.AuthenticationScheme, builder =>
                {
                    builder.DisplayName = "Facebook";
                    builder.HandlerType = typeof(FacebookHandler);
                });   
            }
        }

        public void Configure(string name, FacebookOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (!string.Equals(name, FacebookDefaults.AuthenticationScheme))
            {
                return;
            }

            var settings = _appContext.Services.Resolve<FacebookExternalAuthSettings>();
            if (settings.ClientKeyIdentifier.HasValue() && settings.ClientSecret.HasValue())
            {
                options.AppId = settings.ClientKeyIdentifier;
                options.AppSecret = settings.ClientSecret;
            }

            options.Events = new OAuthEvents
            {
                OnRemoteFailure = context =>
                {
                    var errorUrl = context.Request.PathBase.Value + $"/identity/externalerrorcallback?provider=facebook&errorMessage={context.Failure.Message}";
                    context.Response.Redirect(errorUrl);
                    context.HandleResponse();

                    return Task.CompletedTask;
                }
            };

            // TODO: (mh) (core) This must also be called when setting is changing via all settings grid.
        }

        public void Configure(FacebookOptions options) => Debug.Fail("This infrastructure method shouldn't be called.");
    }
}
