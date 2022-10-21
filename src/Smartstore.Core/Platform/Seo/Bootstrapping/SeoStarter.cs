using System;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class SeoStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<UrlService>().As<IUrlService>().InstancePerLifetimeScope();
            builder.Register<UrlPolicy>(x => x.Resolve<IUrlService>().GetUrlPolicy()).InstancePerLifetimeScope();
            builder.RegisterType<XmlSitemapGenerator>().As<IXmlSitemapGenerator>().InstancePerLifetimeScope();
            builder.RegisterType<CanonicalHostUrlFilter>().As<IUrlFilter>().SingleInstance();
        }

        public override void BuildPipeline(RequestPipelineBuilder builder)
        {
            builder.Configure(StarterOrdering.AfterAuthenticationMiddleware - 2, app =>
            {
                if (builder.ApplicationContext.IsInstalled)
                {
                    app.UseUrlPolicy();
                }
            });
        }
    }
}