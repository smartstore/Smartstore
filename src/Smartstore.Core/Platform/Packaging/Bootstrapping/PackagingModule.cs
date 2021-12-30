using System;
using Autofac;
using Smartstore.Core.Packaging;
using Smartstore.Engine;

namespace Smartstore.Core.Bootstrapping
{
    internal class PackagingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PackageBuilder>()
                .As<IPackageBuilder>()
                .UsingConstructor(typeof(IApplicationContext))
                .InstancePerLifetimeScope();

            builder.RegisterType<PackageInstaller>().As<IPackageInstaller>().InstancePerLifetimeScope();
            builder.RegisterType<UpdateChecker>().AsSelf().InstancePerLifetimeScope();
        }
    }
}
