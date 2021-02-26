using System;
using Autofac;
using Smartstore.Core.Seo;
using Smartstore.Core.Security;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public class SecurityStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<HttpsUrlFilter>().As<IUrlFilter>().InstancePerLifetimeScope();
            builder.RegisterType<Encryptor>().As<IEncryptor>().InstancePerLifetimeScope();
            builder.RegisterType<HoneypotProtector>().SingleInstance();

            if (appContext.IsInstalled)
            {
                builder.RegisterType<PermissionService>().As<IPermissionService>().InstancePerLifetimeScope();
                builder.RegisterType<AclService>().As<IAclService>().InstancePerLifetimeScope();
            }
        }
    }
}
