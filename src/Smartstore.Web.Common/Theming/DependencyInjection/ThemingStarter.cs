using System;
using Autofac;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Events;
using Smartstore.Web.TagHelpers;
using Smartstore.Web.Razor;

namespace Smartstore.Web.Theming.DependencyInjection
{
    public sealed class ThemingStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
        {
            return appContext.IsInstalled;
        }

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