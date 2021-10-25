using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Twitter.Bootstrapping;

namespace Smartstore.Twitter.Auth
{
    internal class Startup : StarterBase
    {
        // TODO: (mh) (core) Adapt from final implementation of Facebook plugin.

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Transient<IConfigureOptions<AuthenticationOptions>, TwitterOptionsConfigurer>(),
                ServiceDescriptor.Transient<IConfigureOptions<TwitterOptions>, TwitterOptionsConfigurer>(),
                ServiceDescriptor.Transient<IConfigureNamedOptions<TwitterOptions>, TwitterOptionsConfigurer>(),
                ServiceDescriptor.Transient<IPostConfigureOptions<TwitterOptions>, TwitterPostConfigureOptions>()
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            // INFO: (mh) (core) When registering like this. Smartstore throws somewhere in RequestCultureMiddleware. I wasn't able to detect why...
            //builder.RegisterType<TwitterOptionsConfigurer>()
            //    .As<IConfigureOptions<AuthenticationOptions>>()
            //    .As<IConfigureOptions<TwitterOptions>>()
            //    .As<IConfigureNamedOptions<TwitterOptions>>()
            //    .InstancePerDependency();

            // INFO: (mh) (core) This won't work as TwitterOptions are inherited from RemoteAuthenticationOptions and not from OAuthOptions
            //builder.RegisterType<OAuthPostConfigureOptions<TwitterOptions, TwitterHandler>>()
            //    .As<IPostConfigureOptions<TwitterOptions>>()
            //    .InstancePerDependency();
        }
    }
}
