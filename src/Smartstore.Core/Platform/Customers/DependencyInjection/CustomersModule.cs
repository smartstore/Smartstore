using System;
using Autofac;

namespace Smartstore.Core.Customers.DependencyInjection
{
    public sealed class CustomersModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();
        }
    }
}