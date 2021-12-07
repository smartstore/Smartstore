using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Microsoft.Auth
{
    internal class Startup : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<MicrosoftAccountOptionsConfigurer>()
                .As<IConfigureOptions<AuthenticationOptions>>()
                .As<IConfigureOptions<MicrosoftAccountOptions>>()
                .InstancePerDependency();

            builder.RegisterType<OAuthPostConfigureOptions<MicrosoftAccountOptions, MicrosoftAccountHandler>>()
                .As<IPostConfigureOptions<MicrosoftAccountOptions>>()
                .InstancePerDependency();
        }
    }
}
