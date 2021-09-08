using System;
using Autofac;
using Smartstore.Blog.Services;
using Smartstore.Core.Content.Menus;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Blog
{
    internal class Startup : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<BlogService>().As<IBlogService>().InstancePerLifetimeScope();
            builder.RegisterType<BlogLinkProvider>().As<ILinkProvider>().InstancePerLifetimeScope();
        }
    }
}
