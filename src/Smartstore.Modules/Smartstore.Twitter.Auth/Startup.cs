using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Twitter.Auth
{
    internal class Startup : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<TwitterOptionsConfigurer>()
                .As<IConfigureOptions<AuthenticationOptions>>()
                .As<IConfigureOptions<TwitterOptions>>()
                .InstancePerDependency();

            builder.RegisterType<TwitterPostConfigureOptions>()
                .As<IPostConfigureOptions<TwitterOptions>>()
                .InstancePerDependency();
        }
    }
}
