using System;
using Autofac;
using Smartstore.Core.Packaging;

namespace Smartstore.Core.Bootstrapping
{
    internal class PackagingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PackageBuilder>().As<IPackageBuilder>().InstancePerLifetimeScope();
            builder.RegisterType<PackageInstaller>().As<IPackageInstaller>().InstancePerLifetimeScope();
            builder.RegisterType<UpdateChecker>().AsSelf().InstancePerLifetimeScope();
        }
    }
}
