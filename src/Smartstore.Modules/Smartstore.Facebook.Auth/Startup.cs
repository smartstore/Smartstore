using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Facebook.Bootstrapping;

namespace Smartstore.Facebook.Auth
{
    internal class Startup : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<FacebookOptionsConfigurer>()
                .As<IConfigureOptions<AuthenticationOptions>>()
                .As<IConfigureOptions<FacebookOptions>>()
                .As<IConfigureNamedOptions<FacebookOptions>>()
                .InstancePerDependency();

            builder.RegisterType<OAuthPostConfigureOptions<FacebookOptions, FacebookHandler>>()
                .As<IPostConfigureOptions<FacebookOptions>>()
                .InstancePerDependency();
        }
    }
}
