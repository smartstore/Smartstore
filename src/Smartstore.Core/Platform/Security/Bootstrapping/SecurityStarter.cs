using System;
using Autofac;
using Smartstore.Core.Security;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public class SecurityStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<PermissionService>().As<IPermissionService>().InstancePerLifetimeScope();
        }
    }
}
