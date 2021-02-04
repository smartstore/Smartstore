using System;
using Autofac;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class CustomersModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();
        }
    }
}