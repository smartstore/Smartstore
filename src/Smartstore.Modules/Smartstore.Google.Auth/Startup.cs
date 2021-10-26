using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Google.Bootstrapping;

namespace Smartstore.Google.Auth
{
    internal class Startup : StarterBase
    {
        // TODO: (mh) (core) Adapt from final implementation of Facebook plugin.
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Transient<IConfigureOptions<AuthenticationOptions>, GoogleOptionsConfigurer>(),
                ServiceDescriptor.Transient<IConfigureOptions<GoogleOptions>, GoogleOptionsConfigurer>(),
                ServiceDescriptor.Transient<IConfigureNamedOptions<GoogleOptions>, GoogleOptionsConfigurer>(),
                ServiceDescriptor.Transient<IPostConfigureOptions<GoogleOptions>, OAuthPostConfigureOptions<GoogleOptions,GoogleHandler>>()
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // INFO: (mh) (core) When registering like this. Smartstore throws somewhere in RequestCultureMiddleware. I wasn't able to detect why...
            //builder.RegisterType<GoogleOptionsConfigurer>()
            //    .As<IConfigureOptions<AuthenticationOptions>>()
            //    .As<IConfigureOptions<GoogleOptions>>()
            //    .As<IConfigureNamedOptions<GoogleOptions>>()
            //    .InstancePerDependency();

            //builder.RegisterType<OAuthPostConfigureOptions<GoogleOptions, GoogleHandler>>()
            //    .As<IPostConfigureOptions<GoogleOptions>>()
            //    .InstancePerDependency();
        }
    }
}
