using Autofac;
using Smartstore.Admin.Controllers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Admin.Infrastructure
{
    internal class AdminStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            if (appContext.IsInstalled)
            {
                builder.RegisterType<AdminModelHelper>().InstancePerLifetimeScope();
            }
        }
    }
}
