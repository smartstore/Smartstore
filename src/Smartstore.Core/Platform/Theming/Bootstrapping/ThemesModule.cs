using Autofac;
using Smartstore.Core.Theming;
using Smartstore.Events;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class ThemesModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(x => new DefaultThemeRegistry(x.Resolve<IEventPublisher>(), x.Resolve<IApplicationContext>(), null, true))
                .As<IThemeRegistry>()
                .SingleInstance();
        }
    }
}
