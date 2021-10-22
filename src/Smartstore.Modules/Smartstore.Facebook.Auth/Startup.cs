using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Facebook.Auth.Filters;
using Smartstore.Facebook.Bootstrapping;

namespace Smartstore.Facebook.Auth
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Transient<IConfigureOptions<AuthenticationOptions>, FacebookOptionsConfigurer>(),
                ServiceDescriptor.Transient<IConfigureOptions<FacebookOptions>, FacebookOptionsConfigurer>(),
                ServiceDescriptor.Transient<IConfigureNamedOptions<FacebookOptions>, FacebookOptionsConfigurer>(),
                ServiceDescriptor.Transient<IPostConfigureOptions<FacebookOptions>, OAuthPostConfigureOptions<FacebookOptions,FacebookHandler>>()
            });

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<LoginButtonFilter>(
                    context => context.RouteData.Values.IsSameRoute("Identity", "Login"));
            });
        }
    }
}
