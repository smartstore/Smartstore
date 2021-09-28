using System;
using Autofac;
using Smartstore.Core.Messaging;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Infrastructure
{
    internal class PublicWebStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            if (appContext.IsInstalled)
            {
                builder.RegisterType<CatalogHelper>().InstancePerLifetimeScope();
                builder.RegisterType<OrderHelper>().InstancePerLifetimeScope();
                builder.RegisterType<MessageModelHelper>().InstancePerLifetimeScope();
            }
        }
    }
}
