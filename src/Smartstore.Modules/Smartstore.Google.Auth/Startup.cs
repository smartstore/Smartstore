using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Google.Bootstrapping;

namespace Smartstore.Google.Auth
{
    internal class Startup : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<GoogleOptionsConfigurer>()
                .As<IConfigureOptions<AuthenticationOptions>>()
                .As<IConfigureOptions<GoogleOptions>>()
                .InstancePerDependency();

            builder.RegisterType<OAuthPostConfigureOptions<GoogleOptions, GoogleHandler>>()
                .As<IPostConfigureOptions<GoogleOptions>>()
                .InstancePerDependency();
        }
    }
}