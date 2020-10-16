using System;
using Autofac;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Web.Common.TagHelpers;
using Smartstore.Web.Common.Theming.Razor;

namespace Smartstore.Web.Common.Theming.DependencyInjection
{
    public sealed class ThemingStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.Register(x => new DefaultThemeRegistry(x.Resolve<IEventPublisher>(), x.Resolve<IApplicationContext>(), null, true))
                .As<IThemeRegistry>()
                .SingleInstance();
            
            builder.RegisterType<DefaultThemeFileResolver>()
                .As<IThemeFileResolver>()
                .SingleInstance();

            // Razor
            builder.RegisterType<LanguageDirTagHelperComponent>()
                .As<ITagHelperComponent>()
                .InstancePerLifetimeScope();

            builder.RegisterType<RazorViewRenderer>()
                .As<IRazorViewRenderer>()
                .InstancePerLifetimeScope();
        }
    }
}