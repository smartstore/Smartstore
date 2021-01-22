using Autofac;
using Smartstore.Core.Security;

namespace Smartstore.Core.DependencyInjection
{
    public sealed class SecurityModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AclService>().As<IAclService>().InstancePerLifetimeScope();
        }
    }
}
