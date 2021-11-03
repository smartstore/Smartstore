using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Microsoft.Bootstrapping;

namespace Smartstore.Microsoft.Auth
{
    // TODO: (mh) (core) Please define decent (unlocalized) friendly names and descriptions for all auth modules (module.json).
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
