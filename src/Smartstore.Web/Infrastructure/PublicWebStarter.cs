using Autofac;
using Smartstore.Admin.Models.Customers;
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

                // TODO: (mh) (core) Consider implementing an AdminWebStarter as this helper is only used in admin area.
                builder.RegisterType<CustomerHelper>().InstancePerLifetimeScope();
            }
        }
    }
}
