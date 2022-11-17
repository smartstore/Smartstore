using Autofac;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class SecurityStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<HttpsUrlFilter>().As<IUrlFilter>().SingleInstance();
            builder.RegisterType<Encryptor>().As<IEncryptor>().InstancePerLifetimeScope();
            builder.RegisterType<HoneypotProtector>().SingleInstance();
            builder.RegisterType<PermissionService>().As<IPermissionService>().InstancePerLifetimeScope();
            builder.RegisterType<AclService>().As<IAclService>().InstancePerLifetimeScope();
        }
    }
}
