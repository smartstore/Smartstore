using Autofac;
using Smartstore.Core.Content.Menus;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public class MenuStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<MenuStorage>().As<IMenuStorage>().InstancePerLifetimeScope();
            builder.RegisterType<LinkResolver>().As<ILinkResolver>().InstancePerLifetimeScope();
        }
    }
}