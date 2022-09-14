using System.Diagnostics;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Smartstore.Engine;
using Smartstore.Twitter.Auth;

namespace Smartstore.Twitter
{
    internal sealed class TwitterOptionsConfigurer : IConfigureOptions<AuthenticationOptions>, IConfigureNamedOptions<TwitterOptions>
    {
        private readonly IApplicationContext _appContext;

        public TwitterOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(AuthenticationOptions options)
        {
            // Register the OpenID Connect client handler in the authentication handlers collection.
            options.AddScheme(TwitterDefaults.AuthenticationScheme, builder =>
            {
                builder.DisplayName = "Twitter";
                builder.HandlerType = typeof(TwitterHandler);
            });
        }

        public void Configure(string name, TwitterOptions options)
        {
            // Ignore OpenID Connect client handler instances that don't correspond to the instance managed by the OpenID module.
            if (name.HasValue() && !string.Equals(name, TwitterDefaults.AuthenticationScheme))
            {
                return;
            }

            var settings = _appContext.Services.Resolve<TwitterExternalAuthSettings>();
            options.ConsumerKey = settings.ConsumerKey;
            options.ConsumerSecret = settings.ConsumerSecret;
            options.RetrieveUserDetails = true;                 // Important setting to retrieve email in response.

            options.Events = new TwitterEvents
            {
                OnRemoteFailure = context =>
                {
                    // INFO: Unlike with the other providers. Wrong client id results in a direct response (401 (Unauthorized)) which won't be handled by this,
                    var errorUrl = context.Request.PathBase.Value + $"/identity/externalerrorcallback?provider=twitter&errorMessage={context.Failure.Message}";
                    context.Response.Redirect(errorUrl);
                    context.HandleResponse();

                    return Task.CompletedTask;
                }
            };

            // TODO: (mh) (core) This must also be called when setting is changing via all settings grid.
        }

        public void Configure(TwitterOptions options)
            => Debug.Fail("This infrastructure method shouldn't be called.");
    }
}
